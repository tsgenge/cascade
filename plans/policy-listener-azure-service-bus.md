# ASB Policy Listener

Add an `IHostedService`-based policy listener that reads `EventEnvelope` messages from an Azure Service Bus topic subscription and dispatches them to `IPolicyDispatcher`. The transport is abstracted behind `IMessageReceiver` so the hosted-service layer remains infrastructure-agnostic. A new infrastructure project (`CascadeEsdm.Messaging.AzureServiceBus`) provides the ASB implementation, wired up via the existing builder pattern.

## For Future Agents
As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done, set its status to `Complete` and write its **Phase Summary** (what was done, key decisions, anything needed to continue with zero context); run the phase's **Verification Plan** and record the result before moving on. When all phases are done, fill in **Final Recap** and **Deployment Plan**.

---

## Phase 1: SharedKernel.Abstractions — messaging abstractions
Status: Complete

### What to build
New types under `src/shared/CascadeEsdm.SharedKernel.Abstractions/Infrastructure/Messaging/`:

- [x] `Message` record — `Body (string)`, `ApplicationProperties (IReadOnlyDictionary<string, string>)`
- [x] `MessageAction` enum — `Complete`, `Abandon`, `DeadLetter`, `Schedule`
- [x] `IMessageReceiver` interface:
  - `Task StartAsync(Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken)`
  - `Task StopAsync(CancellationToken cancellationToken)`
  - `Task ApplyActionAsync(Message message, MessageAction action, CancellationToken cancellationToken)`
- [x] `IMessageExceptionHandler` interface:
  - `Task<MessageAction> HandleAsync(Message message, Exception exception, CancellationToken cancellationToken)`

### Rename
- [x] Rename `DefaultSerialisationSettings.ForServiceBusPublishing()` → `ForMessageBus()` in `src/shared/CascadeEsdm.SharedKernel/Infrastructure/Serialisation/DefaultSerialisationSettings.cs` (update all callers)

### Verification Plan
- `dotnet build src/shared/CascadeEsdm.SharedKernel.Abstractions/CascadeEsdm.SharedKernel.Abstractions.csproj` — expect **Build succeeded, 0 error(s)**
- `dotnet build src/shared/CascadeEsdm.SharedKernel/CascadeEsdm.SharedKernel.csproj` — expect **Build succeeded, 0 error(s)** (rename caller compiles)
- `dotnet test tests/CascadeEsdm.SharedKernel.UnitTests/CascadeEsdm.SharedKernel.UnitTests.csproj --no-build` — all tests pass

### Phase Summary
Added four new types under `SharedKernel.Abstractions/Infrastructure/Messaging/`: `Message` record (using class-style syntax for `netstandard2.1` compatibility), `MessageAction` enum, `IMessageReceiver` interface, and `IMessageExceptionHandler` interface. Renamed `DefaultSerialisationSettings.ForServiceBusPublishing()` to `ForMessageBus()` across the source, docs, and AIContext files. All builds pass; all 67 SharedKernel unit tests pass.

---

## Phase 2: WriteModel — PolicyListener and DefaultMessageExceptionHandler
Status: Complete

### What to build
New types in `src/write/CascadeEsdm.WriteModel/`:

- [x] `DefaultMessageExceptionHandler : IMessageExceptionHandler` (in `Policies/`) — always returns `MessageAction.DeadLetter`
- [x] `PolicyListener : IHostedService` (in `Policies/`):
  - Constructor: `IPolicyDispatcher`, `IMessageReceiver`, `IMessageExceptionHandler`, `ILogger<PolicyListener>`, `JsonSerializerOptions`
  - `StartAsync`: calls `IMessageReceiver.StartAsync(HandleMessageAsync, ct)`
  - `StopAsync`: calls `IMessageReceiver.StopAsync(ct)`
  - `HandleMessageAsync(Message, ct)`:
    - Deserialise `Message.Body` → `EventEnvelope` using the injected `JsonSerializerOptions`
    - Call `IPolicyDispatcher.DispatchAsync(envelope, ct)`
    - On any exception: call `IMessageExceptionHandler.HandleAsync(message, ex, ct)` → call `IMessageReceiver.ApplyActionAsync(message, action, ct)`
    - On success: call `IMessageReceiver.ApplyActionAsync(message, MessageAction.Complete, ct)`

### Extend WriteModelBuilderExtensions
- [x] Add `WithPolicyListener(Action<PolicyListenerBuilder> configure)` extension on `WriteModelBuilder` in `src/write/CascadeEsdm.WriteModel/Composition/WriteModelBuilderExtensions.cs`
- [x] Add `PolicyListenerBuilder` in `src/write/CascadeEsdm.WriteModel/Composition/`:
  - `WithSerialisationSettings(JsonSerializerOptions options)` — overrides default `DefaultSerialisationSettings.ForMessageBus()`
  - `Build(IServiceCollection services)` — validates `IPolicyDispatcher` is registered (throws `InvalidOperationException` if not); validates `IMessageReceiver` is registered (throws if not); registers `PolicyListener` via `AddHostedService`; registers `DefaultMessageExceptionHandler` as `IMessageExceptionHandler` if not already registered

### Verification Plan
- `dotnet build src/write/CascadeEsdm.WriteModel/CascadeEsdm.WriteModel.csproj` — expect **Build succeeded, 0 error(s)**
- `dotnet build Cascade.Esdm.slnx` — full solution builds clean

### Phase Summary
Added `DefaultMessageExceptionHandler` (always returns `DeadLetter`) and `PolicyListener : IHostedService` to `WriteModel/Policies/`. The listener deserialises `Message.Body` → `EventEnvelope`, dispatches via `IPolicyDispatcher`, completes on success, and delegates to `IMessageExceptionHandler` on failure. Added `PolicyListenerBuilder` with `WithSerialisationSettings` override and validation that `IPolicyDispatcher` and `IMessageReceiver` are registered. Added `WithPolicyListener` extension on `WriteModelBuilder`. Added `Microsoft.Extensions.Hosting.Abstractions` (10.0.3) package reference. Both WriteModel project and full solution build cleanly.

---

## Phase 3: Infrastructure — CascadeEsdm.Messaging.AzureServiceBus
Status: Not started

### What to build
New project at `src/infrastructure/CascadeEsdm.Messaging.AzureServiceBus/`:

- [ ] `CascadeEsdm.Messaging.AzureServiceBus.csproj`:
  - `PackageId`: `CascadeEsdm.Messaging.AzureServiceBus`
  - NuGet reference: `Azure.Messaging.ServiceBus` (latest stable)
  - Project references: `CascadeEsdm.SharedKernel`, `CascadeEsdm.WriteModel`
  - `docs/README.md` and `docs/icon.jpg` (copy icon from another infra project)
- [ ] `AzureServiceBusReceiver : IMessageReceiver` — wraps `ServiceBusProcessor`; maps `ServiceBusReceivedMessage` → `Message` (body as string, `ApplicationProperties` filtered to `string` values); routes `ApplyActionAsync` switch to the appropriate `ServiceBusMessageActions` call
- [ ] `ServiceBusReceiverBuilder`:
  - `WithConnectionString(string)`
  - `WithTopic(string)`
  - `WithSubscription(string)`
  - `Build(IServiceCollection)` — validates all three are set; registers `AzureServiceBusReceiver` as `IMessageReceiver` (singleton or scoped — decide: singleton, because `ServiceBusProcessor` is long-lived)
- [ ] `InfrastructureBuilderExtensions.UsingAzureServiceBusPolicyListener(this InfrastructureBuilder, Action<ServiceBusReceiverBuilder>)` — creates builder, calls configure, calls `Build`
- [ ] Add project to `Cascade.Esdm.slnx`

### Verification Plan
- `dotnet build src/infrastructure/CascadeEsdm.Messaging.AzureServiceBus/CascadeEsdm.Messaging.AzureServiceBus.csproj` — expect **Build succeeded, 0 error(s)**
- `dotnet build Cascade.Esdm.slnx` — full solution builds clean

### Phase Summary
_(write when phase completes)_

---

## Phase 4: Tests
Status: Not started

### CascadeEsdm.WriteModel.Tests — PolicyListener
- [ ] `PolicyListener_WhenMessageReceived_DeserialisesAndDispatchesToPolicyDispatcher`
- [ ] `PolicyListener_WhenDispatchSucceeds_CompletesMessage`
- [ ] `PolicyListener_WhenDispatchThrows_CallsExceptionHandler`
- [ ] `PolicyListener_WhenExceptionHandlerReturnsDeadLetter_DeadLettersMessage`
- [ ] `PolicyListener_WhenExceptionHandlerReturnsAbandon_AbandonsMessage`
- [ ] `PolicyListener_WhenCancelled_StopsReceiver`

### CascadeEsdm.WriteModel.Tests — DefaultMessageExceptionHandler
- [ ] `DefaultMessageExceptionHandler_Always_ReturnsDeadLetter`

### CascadeEsdm.WriteModel.Tests — PolicyListenerBuilder (composition validation)
- [ ] `WithPolicyListener_WhenPolicyDispatcherNotRegistered_ThrowsInvalidOperationException`
- [ ] `WithPolicyListener_WhenMessageReceiverNotRegistered_ThrowsInvalidOperationException`
- [ ] `WithPolicyListener_WhenAllDependenciesPresent_RegistersPolicyListenerAsHostedService`
- [ ] `WithPolicyListener_WhenNoExceptionHandlerRegistered_RegistersDefaultMessageExceptionHandler`
- [ ] `WithPolicyListener_WhenCustomExceptionHandlerRegistered_DoesNotOverrideIt`

### CascadeEsdm.Messaging.AzureServiceBus (new test project or inline builder tests)
- [ ] `ServiceBusReceiverBuilder_WhenConnectionStringMissing_ThrowsInvalidOperationException`
- [ ] `ServiceBusReceiverBuilder_WhenTopicMissing_ThrowsInvalidOperationException`
- [ ] `ServiceBusReceiverBuilder_WhenSubscriptionMissing_ThrowsInvalidOperationException`
- [ ] `ServiceBusReceiverBuilder_WhenAllSet_RegistersAzureServiceBusReceiverAsIMessageReceiver`

### Verification Plan
- `dotnet test tests/CascadeEsdm.WriteModel.Tests/CascadeEsdm.WriteModel.Tests.csproj` — all tests pass, including new ones
- `dotnet test Cascade.Esdm.slnx` (or equivalent full test run) — all tests pass across solution

### Phase Summary
_(write when phase completes)_

---

## Phase 5: Documentation
Status: Not started

- [ ] **Root README** — add `CascadeEsdm.Messaging.AzureServiceBus` to the packages table with a one-liner description
- [ ] **`/docs/policy-listener.md`** — new file covering: what the policy listener is, composition API (`WithPolicies` + `WithPolicyListener`), `IMessageReceiver` abstraction, `IMessageExceptionHandler` contract, `MessageAction` values, `DefaultMessageExceptionHandler`, the ASB infrastructure package (`UsingAzureServiceBusPolicyListener`), and a complete composition example
- [ ] **`AIContext/ai-context/`** — update the context file to cover the policy listener pattern: how to wire it up, the abstractions, and the default exception handler behaviour

### Verification Plan
- Open `/docs/policy-listener.md` and confirm it renders correctly (headings, code blocks, no broken links)
- Verify the root README package table includes `CascadeEsdm.Messaging.AzureServiceBus`
- Verify the AIContext file references `IMessageReceiver`, `IMessageExceptionHandler`, `MessageAction`, and `PolicyListener`

### Phase Summary
_(write when phase completes)_

---

## Final Recap
_(write when all phases complete)_

## Deployment Plan
_(write when all phases complete)_
