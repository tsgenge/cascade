# Keyed Policy Partitions for Policy Listeners

Allow `UsingPolicies` to be keyed by a string so that each keyed `PolicyListener` dispatches only the policies registered under the same key. Policies registered without a key form a shared default pool for unkeyed listeners.

## For Future Agents

As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done, set its status to `Complete` and write its **Phase Summary** (what was done, key decisions, anything needed to continue with zero context); run the phase's **Verification Plan** and record the result before moving on. When all phases are done, fill in **Final Recap** and **Deployment Plan**.

---

## Design Summary

### The DI problem

Currently `UsingPolicies` registers every `IPolicy` as an unkeyed service, and every `PolicyListener` resolves the same unkeyed `IPolicyDispatcher`. This means every listener runs every policy. We need to keep the existing shared default behaviour while also letting a listener with a matching key run an isolated policy set.

### Chosen solution: keyed `IPolicy` + keyed `IPolicyDispatcher`

`Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.3 (already referenced) supports keyed service registration and resolution (`AddKeyedScoped`, `GetKeyedServices<T>`, `GetRequiredKeyedService<T>`).

- `UsingPolicies(string? key, Action<PolicyBuilder>)` registers all policies inside the block under `key` when it is non-null; when it is null the policies remain unkeyed (shared default pool).
- `PolicyBuilder` stores the key and uses it for `AddPolicy<T>`, `AddPoliciesFromAssembly<T>`, and `AddPoliciesFromNamespace<T>`.
- `UsingPolicies` registers an `IPolicyDispatcher` matching the key:
  - null key: unchanged `AddScoped<IPolicyDispatcher, PolicyDispatcher>()`.
  - non-null key: `AddKeyedScoped<IPolicyDispatcher>(key, factory)` where the factory resolves `IEnumerable<IPolicy>` via `GetKeyedServices<IPolicy>(key)` so the dispatcher only sees the partitioned policies.
- `PolicyListenerBuilder` validates that a dispatcher matching the listener name exists and resolves it via `GetRequiredKeyedService<IPolicyDispatcher>(name)`. Passing `null` resolves the unkeyed shared dispatcher.

### Resulting composition API

```csharp
// Unkeyed (default) listener gets the shared default policies
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        // ...
        .UsingAzureServiceBusReceiver(asb => asb
            .WithConnectionString(conn1)
            .WithTopic("domain-events")
            .WithSubscription("policy-handler"))
        .UsingAzureServiceBusReceiver("orders", asb => asb
            .WithConnectionString(conn2)
            .WithTopic("orders")
            .WithSubscription("policy-handler")))
    .WithWriteModel(write => write
        // ...
        .UsingPolicies(p => p
            .AddPolicy<SharedDefaultPolicy>())            // shared default pool
        .UsingPolicies("orders", p => p
            .AddPolicy<OrderPolicy>())                   // isolated to "orders" listener
        .AddPolicyListener()                              // runs SharedDefaultPolicy
        .AddPolicyListener("orders")));
```

### Backwards compatibility

- `UsingPolicies(Action<PolicyBuilder>)` remains and registers the shared default pool.
- `AddPolicyListener()` remains and binds to the unkeyed shared dispatcher.
- Existing single-listener/single-policy consumers require zero changes.

### Partitioning rules

- A keyed listener resolves the `IPolicyDispatcher` keyed with the same name; it sees **only** policies registered with that same key.
- An unkeyed listener resolves the unkeyed `IPolicyDispatcher`; it sees **only** policies registered without a key.
- If a keyed listener has no matching keyed `IPolicyDispatcher` registration, `PolicyListenerBuilder.Build()` throws `InvalidOperationException` with a clear message.
- Multiple `UsingPolicies("sameKey", ...)` calls aggregate the policies for that key (standard keyed DI behaviour).

---

## Phase 1: Update `PolicyBuilder` to accept a partition key
Status: Complete

- [x] Add a `string? _key` field to `PolicyBuilder` (`src/write/CascadeEsdm.WriteModel/Composition/PolicyBuilder.cs`).
- [x] Add a constructor overload `PolicyBuilder(IServiceCollection services, string? key)` and keep the existing constructor for unkeyed registration.
- [x] Update `AddPolicy<TPolicy>()` to use `AddKeyedScoped<IPolicy, TPolicy>(_key)` when `_key` is non-null, otherwise `AddScoped<IPolicy, TPolicy>()`.
- [x] Update `RegisterPolicies` to use `AddKeyedScoped(typeof(IPolicy), _key, policyType)` when `_key` is non-null, otherwise `AddScoped(typeof(IPolicy), policyType)`.
- [x] Ensure existing unkeyed registration still works exactly as before.

### Verification Plan

- Run the existing `PolicyBuilder`/`PolicyListenerBuilder` unit tests:
  ```powershell
  dotnet test tests\CascadeEsdm.WriteModel.Tests\CascadeEsdm.WriteModel.Tests.csproj --filter "FullyQualifiedName~PolicyBuilderTests|FullyQualifiedName~PolicyListenerBuilderTests"
  ```
  Expected: all pass (backwards compatibility preserved).
- Add a new unit test verifying that `PolicyBuilder` with key `orders` registers `IPolicy` as a keyed service with key `"orders"`, and that unkeyed `PolicyBuilder` still registers unkeyed `IPolicy`.

### Phase Summary
Added a private `string? _key` to `PolicyBuilder`, a `PolicyBuilder(IServiceCollection, string?)` constructor, and had the original constructor chain to it with `null`. `AddPolicy<T>()` and `RegisterPolicies` now branch on `_key`: unkeyed uses `AddScoped`, keyed uses `AddKeyedScoped(...)`. Backwards compatibility preserved (keyless path unchanged). New `PolicyBuilderTests` cover unkeyed vs keyed registration and that `GetKeyedServices<IPolicy>(key)` returns only the matching partition. Verified via the WriteModel unit tests (all pass).

---

## Phase 2: Add `UsingPolicies` keyed overload and keyed `IPolicyDispatcher` registration
Status: Complete

- [x] Add `UsingPolicies(string? key, Action<PolicyBuilder>)` to `WriteModelBuilderExtensions` (`src/write/CascadeEsdm.WriteModel/Composition/WriteModelBuilderExtensions.cs`).
- [x] Keep the existing `UsingPolicies(Action<PolicyBuilder>)` overload and have it delegate to the new overload with `key: null`.
- [x] In the keyed overload, construct `PolicyBuilder` with the key and register the dispatcher:
  - null key: `services.AddScoped<IPolicyDispatcher, PolicyDispatcher>()`.
  - non-null key: `services.AddKeyedScoped<IPolicyDispatcher>(key, (sp, serviceKey) => new PolicyDispatcher(sp.GetKeyedServices<IPolicy>(serviceKey).ToList(), sp.GetRequiredService<ILogger<PolicyDispatcher>>()))`.
- [x] Ensure only one `IPolicyDispatcher` registration is added per `UsingPolicies` call (the correct one for the key).
- [x] Remove or update the previous `services.AddScoped<IPolicyDispatcher, PolicyDispatcher>()` that is currently unconditional inside `UsingPolicies`.

### Verification Plan

- Build the solution:
  ```powershell
  dotnet build Cascade.Esdm.slnx
  ```
  Expected: no errors.
- Add unit tests verifying that:
  - `UsingPolicies()` registers an unkeyed `IPolicyDispatcher`.
  - `UsingPolicies("orders", ...)` registers a keyed `IPolicyDispatcher` with key `"orders"`.
  - Resolving the keyed dispatcher returns only the keyed policies.
  - Resolving the unkeyed dispatcher returns only the unkeyed policies.

### Phase Summary
Added `UsingPolicies(string? key, Action<PolicyBuilder>)`; the original keyless overload now delegates with `key: null`. Keyless registers the unkeyed `PolicyDispatcher`; keyed registers `AddKeyedScoped<IPolicyDispatcher>(key, ...)` whose factory builds a `PolicyDispatcher` from `GetKeyedServices<IPolicy>(serviceKey)`, so it only sees its partition. Added `using Microsoft.Extensions.Logging`. `PolicyPartitioningTests` verify unkeyed vs keyed dispatcher registration and that each dispatcher executes only its own partition's policies.

---

## Phase 3: Update `PolicyListenerBuilder` to resolve the keyed dispatcher
Status: Complete

- [x] Update `PolicyListenerBuilder.Build()` validation (`src/write/CascadeEsdm.WriteModel/Composition/PolicyListenerBuilder.cs`) to look for:
  - unkeyed `IPolicyDispatcher` when `_name` is null, or
  - keyed `IPolicyDispatcher` with service key `_name` when `_name` is non-null.
- [x] Update the `IHostedService` factory to resolve `IPolicyDispatcher` via `sp.GetRequiredKeyedService<IPolicyDispatcher>(_name)` (passing `null` resolves the unkeyed dispatcher). **Deviation:** the dispatcher is scoped and is resolved per-message inside `PolicyListener`, not in the hosted-service factory (which runs on the root provider). Instead the listener key is passed to `PolicyListener`.
- [x] Provide clear `InvalidOperationException` messages for missing keyed or unkeyed dispatchers.
- [x] ~~Leave `PolicyListener` unchanged~~ **Deviation (required for correctness):** `PolicyListener` resolved the *unkeyed* `IPolicyDispatcher` from its per-message scope, so a keyed listener would never reach its partition. Added an optional `string? dispatcherKey` constructor parameter; when non-null the listener resolves `GetRequiredKeyedService<IPolicyDispatcher>(key)`, otherwise the unkeyed `GetRequiredService<IPolicyDispatcher>()` (unchanged path). `PolicyListenerBuilder` passes `_name` through.

### Verification Plan

- Run unit tests:
  ```powershell
  dotnet test tests\CascadeEsdm.WriteModel.Tests\CascadeEsdm.WriteModel.Tests.csproj --filter "FullyQualifiedName~PolicyListenerBuilderTests"
  ```
  Expected: all pass, including new tests.
- Add unit tests verifying that:
  - `AddPolicyListener("orders")` throws when no keyed `IPolicyDispatcher` with key `"orders"` is registered.
  - `AddPolicyListener("orders")` succeeds when a keyed `IPolicyDispatcher` with key `"orders"` is registered.
  - `AddPolicyListener()` still succeeds when only an unkeyed `IPolicyDispatcher` is registered.

### Phase Summary
`Build()` now validates the dispatcher registration by key (unkeyed when `_name` is null, keyed matching `_name` otherwise) with distinct `InvalidOperationException` messages, and passes `_name` into `PolicyListener`. `PolicyListener` gained an optional `dispatcherKey` and resolves the keyed dispatcher per-message when it is set. `PolicyListenerBuilderTests` for named listeners were updated to register matching keyed dispatchers; `PolicyPartitioningTests` add the missing-keyed-dispatcher throw and end-to-end routing coverage.

---

## Phase 4: Add partitioning tests
Status: Complete

- [x] Add unit tests in `tests/CascadeEsdm.WriteModel.Tests/UnitTests/Composition/` or `UnitTests/Policies/` that build a service provider with:
  - one unkeyed `UsingPolicies(...)` block registering a shared policy,
  - one keyed `UsingPolicies("orders", ...)` block registering an orders-only policy,
  - another keyed `UsingPolicies("payments", ...)` block registering a payments-only policy.
- [x] Verify that:
  - the unkeyed `IPolicyDispatcher` dispatches only the shared policy,
  - the `"orders"` `IPolicyDispatcher` dispatches only the orders policy,
  - the `"payments"` `IPolicyDispatcher` dispatches only the payments policy.
- [x] Add a test that wires a keyed `PolicyListener` with a matching keyed `IMessageReceiver` and a matching keyed `IPolicyDispatcher`, then dispatches a message and confirms only the keyed partition policies execute.
- [x] Add a test that confirms an unkeyed listener still executes only the unkeyed shared policies when a keyed partition also exists.
- [x] Ensure tests do not depend on real Azure resources; use mocks or the in-memory `ServiceCollection`/`ServiceProvider`.

### Verification Plan

- Run all new and existing policy tests:
  ```powershell
  dotnet test tests\CascadeEsdm.WriteModel.Tests\CascadeEsdm.WriteModel.Tests.csproj --filter "FullyQualifiedName~Policy"
  ```
  Expected: all pass.

### Phase Summary
Added `PolicyPartitioningTests` covering unkeyed vs keyed dispatcher registration, three isolated partitions (shared/orders/payments) each executing only their own policies via a shared `PolicyExecutionRecorder`, the missing-keyed-dispatcher throw, and end-to-end listener routing through a `CapturingMessageReceiver` feeding a serialised `PersonAdded` envelope. All policy tests (39) and all WriteModel unit tests (228) pass.

---

## Phase 5: Update documentation
Status: Complete

- [x] Update `docs/Policies.md` to document the keyed `UsingPolicies` overload and the partition semantics.
- [x] Update `docs/PolicyListener.md` to explain that keyed listeners dispatch only policies from the matching keyed partition, and that unkeyed listeners use the shared default pool.
- [x] Update `docs/CompositionUsage.md` to show the keyed `UsingPolicies` example in the write-model section.
- [x] Keep the root `README.md` unchanged unless it already lists policy features; this is a detailed convention, not a high-level change.
- [x] Follow the project documentation-organization rules (no consumer-facing conventions in `.devin/rules/`; keep per-package READMEs stable).

### Verification Plan

- Review the updated docs for:
  - Correct API examples (`UsingPolicies("orders", ...)` and `AddPolicyListener("orders")`).
  - Clear explanation of the shared default vs. isolated partition behaviour.
  - No broken links or duplicated detailed content.
- No build/test step required, but a quick markdown preview is recommended.

### Phase Summary
Added a "Keyed Policy Partitions" section to `docs/Policies.md`; updated the Validation and Multiple Listeners sections plus the full multi-listener example in `docs/PolicyListener.md`; added a keyed `UsingPolicies` example and note to `docs/CompositionUsage.md`. `README.md`, per-package READMEs, and `.devin/rules/` left unchanged per the documentation-organization rules.

---

## Final Recap
Keyed policy partitioning is implemented across the write-model composition layer:
- `PolicyBuilder` accepts an optional partition key and registers policies as keyed or unkeyed DI services accordingly.
- `UsingPolicies` gained a `(string? key, Action<PolicyBuilder>)` overload; the keyless overload delegates with `null`. Keyed calls register a keyed `IPolicyDispatcher` whose factory only sees `GetKeyedServices<IPolicy>(key)`; keyless registers the shared unkeyed dispatcher.
- `PolicyListenerBuilder.Build()` validates a matching (keyed or unkeyed) `IPolicyDispatcher` and `IMessageReceiver`, and passes the listener key to `PolicyListener`.
- `PolicyListener` resolves the keyed dispatcher per-message when a key is present (unkeyed path unchanged) — the one deviation from the plan, required because the dispatcher is scoped and the original code always resolved the unkeyed dispatcher.
- Tests: new `PolicyBuilderTests` and `PolicyPartitioningTests`; updated `PolicyListenerBuilderTests`. All 228 WriteModel unit tests pass; the solution builds clean in Release.
- Docs updated in `Policies.md`, `PolicyListener.md`, `CompositionUsage.md`.

Backwards compatible: existing single-pool/single-listener consumers need no changes.

## Deployment Plan
No runtime/infra changes or migrations. This is a source-compatible, additive library change.
1. Merge the PR once CI is green.
2. The CI/CD pipeline builds and publishes the `CascadeEsdm.WriteModel` NuGet package (alpha/prerelease as configured).
3. Consumers adopt keyed partitions optionally by upgrading the package and adding `UsingPolicies("key", ...)` + `AddPolicyListener("key")` pairs; no action required for existing usage.

> Breaking-change note: a named `AddPolicyListener("x")` now requires a matching `UsingPolicies("x", ...)` (previously any listener bound to the single shared dispatcher). Any consumer that registered named listeners against the shared pool must add a matching keyed `UsingPolicies` block or make the listener unnamed.
