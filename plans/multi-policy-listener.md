# Multi-Source Policy Listener

Allow multiple `AddPolicyListener` calls (write model) paired with multiple `UsingAzureServiceBusReceiver` calls (infra), each producing an independent `IHostedService` that listens to a distinct Service Bus topic/subscription. All listeners share the same registered `IPolicyDispatcher` and therefore the same set of policies, but each listener gets its own `IMessageReceiver`, `JsonSerializerOptions`, and `IMessageExceptionHandler`.

## For Future Agents
As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done, set its status to `Complete` and write its **Phase Summary** (what was done, key decisions, anything needed to continue with zero context); run the phase's **Verification Plan** and record the result before moving on. When all phases are done, fill in **Final Recap** and **Deployment Plan**.

---

## Design Summary

### The DI problem
`PolicyListener` currently resolves a single `IMessageReceiver` by type. Index/order coupling across infra and write model builder calls is fragile — a misconfiguration silently wires the wrong listener to the wrong topic.

### Chosen solution: named keyed services
`Microsoft.Extensions.DependencyInjection.Abstractions` 8.0+ (package-provided on `netstandard2.1`, already referenced at 10.0.3) supports `AddKeyedSingleton` / `IKeyedServiceProvider`. Both the infra registration and the write model listener registration share a string key — the default (unnamed) listener uses `null` as the key.

- `UsingAzureServiceBusReceiver(string? name, Action<ServiceBusReceiverBuilder>)` registers `IMessageReceiver` as a keyed singleton under `name`.
- `AddPolicyListener(string? name, Action<PolicyListenerBuilder>?)` captures `name` and registers a factory `IHostedService` that resolves `IKeyedServiceProvider.GetRequiredKeyedService<IMessageReceiver>(name)` at runtime.
- A mismatch between the name on the infra side and the name on the write model side produces a clear `InvalidOperationException` at startup — not a silent mis-wiring.
- Recommended pattern: a static constants class in the consuming application.

### `IMessageExceptionHandler` per-listener strategy
Each `AddPolicyListener` call can optionally call `.WithExceptionHandler<THandler>()`. The factory resolves `THandler` from the container (caller must register it). If not set, a `DefaultMessageExceptionHandler` instance is constructed directly inline — no global DI registration.

### Resulting composition API (new)
```csharp
// Recommended: static constants class in your application
public static class PolicyListeners
{
    public const string Orders = "orders";
    public const string Payments = "payments";
}

// infra
.UsingAzureServiceBusReceiver(asb => asb              // default — no name
    .WithConnectionString(conn1)
    .WithTopic("domain-events")
    .WithSubscription("policy-handler"))
.UsingAzureServiceBusReceiver(PolicyListeners.Orders, asb => asb
    .WithConnectionString(conn2)
    .WithTopic("orders")
    .WithSubscription("policy-handler"))
.UsingAzureServiceBusReceiver(PolicyListeners.Payments, asb => asb
    .WithConnectionString(conn3)
    .WithTopic("payments")
    .WithSubscription("policy-handler"))

// write model
.AddPolicyListener()                                        // default — binds to null-keyed receiver
.AddPolicyListener(PolicyListeners.Orders)                  // binds to "orders" receiver
.AddPolicyListener(PolicyListeners.Payments, l => l         // per-listener overrides
    .WithSerialisationSettings(myOptions)
    .WithExceptionHandler<MyExceptionHandler>())
```

### Backwards compatibility
- `UsingAzureServiceBusReceiver(Action<ServiceBusReceiverBuilder>)` (no name) remains — registers under key `null`.
- `UsingPolicyListener(Action<PolicyListenerBuilder>?)` remains — delegates to `AddPolicyListener(null, configure)`.
- Single-listener consumers require zero changes.

---

## Phase 1: Infrastructure — add named overload to ServiceBusReceiverBuilder
Status: Complete

### What to change

**`ServiceBusReceiverBuilder`** (`src/infrastructure/CascadeEsdm.Messaging.AzureServiceBus/ServiceBusReceiverBuilder.cs`):
- Add `string? _name` field; set it via the constructor (internal — called by `InfrastructureBuilderExtensions`).
- In `Build()`: change both singleton registrations to keyed variants:
  ```csharp
  _infraBuilder.Services.AddKeyedSingleton<ServiceBusProcessor>(_name, (_, _) =>
  {
      var client = new ServiceBusClient(_connectionString);
      return client.CreateProcessor(_topic, _subscription);
  });
  _infraBuilder.Services.AddKeyedSingleton<IMessageReceiver>(_name, (sp, key) =>
      new AzureServiceBusReceiver(sp.GetRequiredKeyedService<ServiceBusProcessor>(key)));
  ```
  (`[FromKeyedServices]` on `AzureServiceBusReceiver`'s constructor cannot be used here because the key is dynamic — factory delegates are used instead.)

**`InfrastructureBuilderExtensions`** (`src/infrastructure/CascadeEsdm.Messaging.AzureServiceBus/InfrastructureBuilderExtensions.cs`):
- Add a named overload: `UsingAzureServiceBusReceiver(this InfrastructureBuilder builder, string name, Action<ServiceBusReceiverBuilder> configure)` — passes `name` into `ServiceBusReceiverBuilder`.
- Keep the existing no-name overload; it passes `null` to `ServiceBusReceiverBuilder` (registers under the default `null` key).

- [x] Add `string? _name` to `ServiceBusReceiverBuilder` and update its internal constructor/factory method
- [x] Change `ServiceBusProcessor` registration in `Build()` to `AddKeyedSingleton` using `_name`
- [x] Change `IMessageReceiver` registration in `Build()` to `AddKeyedSingleton` factory using `_name`
- [x] Add named overload `UsingAzureServiceBusReceiver(string name, Action<ServiceBusReceiverBuilder>)` in `InfrastructureBuilderExtensions`

### Verification Plan
- `dotnet build src/infrastructure/CascadeEsdm.Messaging.AzureServiceBus/CascadeEsdm.Messaging.AzureServiceBus.csproj` — expect **Build succeeded, 0 error(s)**
- `dotnet build Cascade.Esdm.slnx` — full solution builds clean

### Phase Summary
Added `string? _name` field to `ServiceBusReceiverBuilder` (set via internal constructor). Changed `Build()` to register `ServiceBusProcessor` and `IMessageReceiver` as keyed singletons using `_name`. Added named overload `UsingAzureServiceBusReceiver(string? name, ...)` in `InfrastructureBuilderExtensions`; existing no-name overload delegates with `null`. Key decision: `AddKeyedSingleton(null, ...)` creates a non-keyed descriptor (`IsKeyedService == false`) but `GetRequiredKeyedService(null)` still resolves it — this is .NET runtime behaviour, handled in Phase 2's guard logic. Both verification builds passed with 0 errors.

---

## Phase 2: WriteModel — refactor PolicyListenerBuilder and WriteModelBuilderExtensions for named listeners
Status: Complete

### What to change

**`PolicyListenerBuilder`** (`src/write/CascadeEsdm.WriteModel/Composition/PolicyListenerBuilder.cs`):
- Add `string? _name` field, set at construction time.
- Add `Type? _exceptionHandlerType` field.
- Add `WithExceptionHandler<THandler>() where THandler : class, IMessageExceptionHandler` — stores `typeof(THandler)`, does not register in DI.
- Refactor `Build()`:
  - Keep the `IPolicyDispatcher` guard.
  - Update the `IMessageReceiver` guard: check that a keyed registration exists for `_name` — `_services.Any(s => s.ServiceType == typeof(IMessageReceiver) && s.IsKeyedService && Equals(s.ServiceKey, _name))`. Error message: `"No IMessageReceiver registered with key '{_name}'. Call UsingAzureServiceBusReceiver with the matching name."`
  - Remove `_services.AddSingleton(options)` — options are closure-captured per listener.
  - Remove `DefaultMessageExceptionHandler` global registration.
  - Register `IHostedService` via a factory:
    ```csharp
    _services.AddTransient<IHostedService>(sp =>
    {
        var keyedProvider = (IKeyedServiceProvider)sp;
        var receiver = keyedProvider.GetRequiredKeyedService<IMessageReceiver>(_name);
        var dispatcher = sp.GetRequiredService<IPolicyDispatcher>();
        var logger = sp.GetRequiredService<ILogger<PolicyListener>>();
        var options = _serializerOptions ?? DefaultSerialisationSettings.ForMessageBus();
        var exceptionHandler = _exceptionHandlerType != null
            ? (IMessageExceptionHandler)sp.GetRequiredService(_exceptionHandlerType)
            : new DefaultMessageExceptionHandler();
        return new PolicyListener(dispatcher, receiver, exceptionHandler, logger, options);
    });
    ```

**`WriteModelBuilderExtensions`** (`src/write/CascadeEsdm.WriteModel/Composition/WriteModelBuilderExtensions.cs`):
- Add `AddPolicyListener(this WriteModelBuilder builder, string? name = null, Action<PolicyListenerBuilder>? configure = null)` extension.
- Update `UsingPolicyListener` to delegate to `AddPolicyListener(null, configure)`.

**`PolicyListener`** (`src/write/CascadeEsdm.WriteModel/Policies/PolicyListener.cs`):
- No changes needed.

- [x] Add `_name` and `_exceptionHandlerType` fields to `PolicyListenerBuilder`
- [x] Add `WithExceptionHandler<THandler>()` to `PolicyListenerBuilder`
- [x] Refactor `PolicyListenerBuilder.Build()`: update `IMessageReceiver` guard, remove global registrations, add keyed-factory `IHostedService`
- [x] Add `AddPolicyListener(string? name, Action<PolicyListenerBuilder>?)` to `WriteModelBuilderExtensions`
- [x] Update `UsingPolicyListener` to delegate to `AddPolicyListener`

### Verification Plan
- `dotnet build src/write/CascadeEsdm.WriteModel/CascadeEsdm.WriteModel.csproj` — expect **Build succeeded, 0 error(s)**
- `dotnet build Cascade.Esdm.slnx` — full solution builds clean

### Phase Summary
Added `_name` and `_exceptionHandlerType` fields to `PolicyListenerBuilder`. Added `WithExceptionHandler<THandler>()` method. Refactored `Build()`: the `IMessageReceiver` guard now uses split logic — for `null` name checks `!s.IsKeyedService`, for non-null checks `s.IsKeyedService && Equals(s.ServiceKey, _name)` (because `AddKeyedSingleton(null, ...)` sets `IsKeyedService = false`). Removed global `AddSingleton(options)` and `DefaultMessageExceptionHandler` registration; both are now closure-captured per factory. Registered `IHostedService` via `AddTransient<IHostedService>(factory)` using `sp.GetRequiredKeyedService<IMessageReceiver>(_name)`. Added `AddPolicyListener` extension on `WriteModelBuilderExtensions`; `UsingPolicyListener` delegates to it. Full solution build passed with 0 errors.

---

## Phase 3: Tests — update and extend tests
Status: Complete

### What to change

**`PolicyListenerBuilderTests`** (`tests/CascadeEsdm.WriteModel.Tests/UnitTests/Composition/PolicyListenerBuilderTests.cs`):

Existing tests and their disposition:
- `UsingPolicyListener_WhenPolicyDispatcherNotRegistered_ThrowsInvalidOperationException` — **keep**, updated receiver registration to use `AddKeyedSingleton((object?)null, ...)`.
- `UsingPolicyListener_WhenMessageReceiverNotRegistered_ThrowsInvalidOperationException` — **update**: the guard now checks for a keyed `IMessageReceiver` with a `null` key, not an unkeyed registration. Register via `services.AddKeyedSingleton<IMessageReceiver>(null, Substitute.For<IMessageReceiver>())` in the positive path; absence of any keyed registration still triggers the error.
- `UsingPolicyListener_WhenAllDependenciesPresent_RegistersPolicyListenerAsHostedService` — **update**: register receiver as keyed (`null` key); assert `IHostedService` registration is a factory (not `ImplementationType == typeof(PolicyListener)` — factory delegates register with `null` ImplementationType).
- `UsingPolicyListener_WhenNoExceptionHandlerRegistered_RegistersDefaultMessageExceptionHandler` — **remove**: `DefaultMessageExceptionHandler` is no longer registered globally in DI; it is constructed inline inside the factory.
- `UsingPolicyListener_WhenCustomExceptionHandlerRegistered_DoesNotOverrideIt` — **remove**: the old global-registration guard is gone; exception handler is specified via `WithExceptionHandler<T>()`, not via pre-registration.

New tests to add:
- `AddPolicyListener_WhenCalledTwiceWithDifferentNames_RegistersTwoHostedServices` — registers two keyed receivers under different names, calls `AddPolicyListener` twice (via `WriteModelBuilderExtensions`), asserts two `IHostedService` factory registrations.
- `AddPolicyListener_WhenReceiverKeyNotRegistered_ThrowsAtBuildTime` — calls `AddPolicyListener("unknown")` without a matching keyed receiver; asserts `InvalidOperationException` mentioning the key name.
- `AddPolicyListener_WhenWithExceptionHandlerCalled_ResolvesSpecifiedType` — calls `.WithExceptionHandler<CustomTestExceptionHandler>()`, builds a service provider, resolves the hosted service, confirms the exception handler injected is the specified type.
- `UsingPolicyListener_BackwardsCompatibility_StillRegistersOneHostedService` — calls the old `UsingPolicyListener()` with a `null`-keyed receiver registered; asserts one `IHostedService` factory is registered.

**`ServiceBusReceiverBuilderTests`** (`tests/CascadeEsdm.WriteModel.Tests/UnitTests/Composition/ServiceBusReceiverBuilderTests.cs`):

Existing tests and their disposition:
- `ServiceBusReceiverBuilder_WhenConnectionStringMissing_ThrowsInvalidOperationException` — **keep**, no changes.
- `ServiceBusReceiverBuilder_WhenTopicMissing_ThrowsInvalidOperationException` — **keep**, no changes.
- `ServiceBusReceiverBuilder_WhenSubscriptionMissing_ThrowsInvalidOperationException` — **keep**, no changes.
- `ServiceBusReceiverBuilder_WhenAllSet_RegistersAzureServiceBusReceiverAsIMessageReceiver` — **update**: assert `IMessageReceiver` is registered as a non-keyed service (`!s.IsKeyedService`) because `AddKeyedSingleton(null, ...)` creates a non-keyed descriptor.

New tests to add:
- `UsingAzureServiceBusReceiver_WhenNamedOverloadUsed_RegistersKeyedReceiverWithMatchingKey` — uses the new named overload with a name string; asserts `IMessageReceiver` keyed under that name is registered.

**`PolicyListenerTests`** (`tests/CascadeEsdm.WriteModel.Tests/UnitTests/Policies/PolicyListenerTests.cs`):
- No changes needed — `PolicyListener` constructor is unchanged.

- [x] Update `UsingPolicyListener_WhenMessageReceiverNotRegistered` to use keyed guard semantics
- [x] Update `UsingPolicyListener_WhenAllDependenciesPresent` to register keyed receiver and assert factory registration
- [x] Remove `UsingPolicyListener_WhenNoExceptionHandlerRegistered_RegistersDefaultMessageExceptionHandler`
- [x] Remove `UsingPolicyListener_WhenCustomExceptionHandlerRegistered_DoesNotOverrideIt`
- [x] Add `AddPolicyListener_WhenCalledTwiceWithDifferentNames_RegistersTwoHostedServices`
- [x] Add `AddPolicyListener_WhenReceiverKeyNotRegistered_ThrowsAtBuildTime`
- [x] Add `AddPolicyListener_WhenWithExceptionHandlerCalled_ResolvesSpecifiedType`
- [x] Add `UsingPolicyListener_BackwardsCompatibility_StillRegistersOneHostedService`
- [x] Update `ServiceBusReceiverBuilder_WhenAllSet_RegistersAzureServiceBusReceiverAsIMessageReceiver` for keyed assertion
- [x] Add `UsingAzureServiceBusReceiver_WhenNamedOverloadUsed_RegistersKeyedReceiverWithMatchingKey`

### Verification Plan
- `dotnet test tests/CascadeEsdm.WriteModel.Tests/CascadeEsdm.WriteModel.Tests.csproj` — all tests pass, including new ones
- `dotnet test Cascade.Esdm.slnx` — all tests pass across solution

### Phase Summary
Updated all existing tests to use `AddKeyedSingleton((object?)null, ...)` for receiver registration and `!s.IsKeyedService` for null-key assertions (because `AddKeyedSingleton(null, ...)` produces `IsKeyedService = false` in .NET). Removed two tests for global exception handler registration. Added four new tests covering multi-listener registration, key mismatch errors, per-listener exception handler resolution, and backwards compatibility. Updated `ServiceBusReceiverBuilderTests` to assert non-keyed for default and keyed for named. All 12 composition tests pass (was 7 before: removed 2, added 4, kept 5, updated 2).

---

## Phase 4: Documentation — update PolicyListener.md, CompositionUsage.md, and AIContext
Status: Complete

### `/docs/PolicyListener.md`
- **Composition section** (line ~40–64): update single-listener example to use `AddPolicyListener` (mention `UsingPolicyListener` is the backwards-compatible alias).
- **Validation section** (line ~66–72): update the `IMessageReceiver` guard description — it now checks for a keyed registration matching the listener name, not an unkeyed singleton.
- **Default Exception Handler section** (line ~74–106): update — `DefaultMessageExceptionHandler` is no longer registered globally; it is constructed inline when no handler type is specified via `WithExceptionHandler<T>()`.
- **Custom Serialisation section** (line ~108–115): update example to use `AddPolicyListener` syntax.
- **Add new section "Multiple Listeners"** (after Custom Serialisation):
  - Explain the named-key pattern.
  - Show the static constants class recommendation.
  - Show the full multi-listener composition example (infra + write model).
  - Document `WithExceptionHandler<THandler>()` per-listener override.
  - Note that key mismatch throws `InvalidOperationException` at startup with a clear message.
- **Azure Service Bus section** (line ~169–197): add the named overload `UsingAzureServiceBusReceiver(string name, ...)` to the configuration reference.

### `/docs/CompositionUsage.md`
- **"Register Policy Listener" section** (line ~148–163): update `UsingPolicyListener()` reference to mention `AddPolicyListener` as the new preferred method; update the builder tree diagram (line ~300) to show `AddPolicyListener()` alongside `UsingPolicyListener()` (alias).

### `src/tools/CascadeEsdm.AIContext/ai-context/cascade-esdm.md`
- **Policy Listener — Composition section** (line ~681–704): update to show `AddPolicyListener` as the primary method; mention `UsingPolicyListener` as the backwards-compatible alias.
- **Policy Listener — Validation section** (line ~707–712): update `IMessageReceiver` guard description for keyed semantics; remove mention of global `DefaultMessageExceptionHandler` auto-registration.
- **Policy Listener — Azure Service Bus section** (line ~723–731): add named overload.
- **Add new "Multiple Listeners" subsection** covering: named-key pattern, static constants recommendation, `WithExceptionHandler<T>()`, full multi-listener composition example.

- [x] Update Composition section in `PolicyListener.md` to use `AddPolicyListener`
- [x] Update Validation section in `PolicyListener.md` for keyed guard
- [x] Update Default Exception Handler section in `PolicyListener.md`
- [x] Update Custom Serialisation example in `PolicyListener.md`
- [x] Add "Multiple Listeners" section to `PolicyListener.md`
- [x] Update Azure Service Bus section in `PolicyListener.md` with named overload
- [x] Update "Register Policy Listener" and builder diagram in `CompositionUsage.md`
- [x] Update Policy Listener Composition + Validation sections in `cascade-esdm.md`
- [x] Update Azure Service Bus section in `cascade-esdm.md` with named overload
- [x] Add "Multiple Listeners" subsection to `cascade-esdm.md`

### Verification Plan
- `dotnet build Cascade.Esdm.slnx` — confirm no broken references
- Open `docs/PolicyListener.md` — confirm all sections render correctly and code examples are consistent
- Verify `cascade-esdm.md` covers the full multi-listener pattern end-to-end

### Phase Summary
Updated all three documentation locations per the documentation-organization rule. `PolicyListener.md`: updated Composition, Validation, Default Exception Handler, and Custom Serialisation sections; added new "Multiple Listeners" section with named-key pattern, static constants recommendation, and full multi-listener composition example; added named overload to Azure Service Bus Configuration. `CompositionUsage.md`: updated Register Policy Listener section and builder tree diagram with `AddPolicyListener` and `UsingPolicyListener` (alias). `cascade-esdm.md`: updated Composition, Validation, and Azure Service Bus sections; added Multiple Listeners subsection with full composition example.

---

## Final Recap
All four phases complete. The refactoring adds multi-source policy listener support using .NET keyed services:

- **Infrastructure**: `ServiceBusReceiverBuilder` accepts a `string? _name` and registers both `ServiceBusProcessor` and `IMessageReceiver` as keyed singletons. `InfrastructureBuilderExtensions` gains a named overload; existing no-name overload delegates with `null`.
- **WriteModel**: `PolicyListenerBuilder` gains `_name`, `_exceptionHandlerType`, and `WithExceptionHandler<T>()`. `Build()` registers `IHostedService` via a factory that resolves the keyed receiver, creating each listener independently. `AddPolicyListener` is the new primary method; `UsingPolicyListener` delegates to it.
- **Tests**: 12 composition tests pass (7 kept/updated, 2 removed, 4 new). Key discovery: `AddKeyedSingleton(null, ...)` sets `IsKeyedService = false` in .NET — guard logic splits on `_name == null` accordingly.
- **Documentation**: All three locations updated consistently per the documentation-organization rule.

Backwards compatibility is preserved: single-listener consumers require zero changes.

## Deployment Plan
Standard NuGet package publish:
1. Merge PR to main.
2. CI/CD pipeline builds and publishes updated packages: `CascadeEsdm.Messaging.AzureServiceBus`, `CascadeEsdm.WriteModel`, `CascadeEsdm.AIContext`.
3. Consumer apps that use `UsingPolicyListener()` continue to work with no changes.
4. To adopt multi-listener, consumers add named `UsingAzureServiceBusReceiver` and `AddPolicyListener` calls.
