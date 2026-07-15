# Partitioned Policy Listener Integration Tests

Add integration tests for the keyed policy-partitioning feature so that real `PolicyListener` + Azure Service Bus emulator wiring is exercised end-to-end. The tests share the same test-container stack (Azurite, Cosmos DB, Service Bus emulator) via a new base environment class, then use three concrete derived environments to model the three partitioning scenarios.

## For Future Agents

As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done, set its status to **Complete** and write its **Phase Summary** (what was done, key decisions, anything needed to continue with zero context); run the phase's **Verification Plan** and record the result before moving on. When all phases are done, fill in **Final Recap** and **Deployment Plan**.

---

## Design Summary

### What already exists

- The WriteModel test project already has an integration-style fixture in `FunctionalTests/Environment/WriteContext.cs` that spins up Azurite, Cosmos DB, and the Service Bus emulator, then builds a host and exposes `IServiceProvider`.
- `PolicyListeningTests.cs` exercises one unkeyed listener that invokes `PersonEatenRemovesPersonPolicy`, using `MessageChannel<RemovePerson>` + `MessageChannelHandler<TCommand>` to capture the command dispatched by the policy.
- Keyed policy partitions were implemented in `plans/policy-listener-keyed-policy-partitions.md`: `UsingPolicies(key, ...)` registers a keyed `IPolicyDispatcher` that only sees policies registered under that key; `AddPolicyListener(key)` wires to a keyed `IMessageReceiver` and dispatches through the matching keyed dispatcher.

### Goal

Move the shared ASB/container infrastructure into an abstract base environment, create three concrete environments (one per scenario), and add test classes that assert policies run only on the partitions/listeners they belong to.

### Scenario mapping

| Scenario | Partitioning setup | Listeners | Expected behaviour |
|---|---|---|---|
| **1. All shared policies** | 3 policies registered in both the unkeyed pool and a keyed `"second-stream"` pool | Unkeyed listener on `example-stream`; keyed listener `"second-stream"` on `second-stream` | All 3 policies execute when a message is received on either stream |
| **2. Mixed shared + partitioned** | 3 unkeyed policies + 3 policies keyed `"partitioned"` | Unkeyed listener on `example-stream`; keyed listener `"partitioned"` on `partitioned-stream` | Unkeyed listener runs only the 3 shared policies; keyed listener runs only the 3 keyed policies |
| **3. Partitioning only** | 3 policies keyed `"partitioned"`, no unkeyed policies | Keyed listener `"partitioned"` on `partitioned-stream` | The 3 keyed policies execute when a message is sent to `partitioned-stream`; sending to `example-stream` produces no executions |

### How to verify policy execution

Use the existing functional-test `MessageChannel<TCommand>` + `MessageChannelHandler<TCommand>` technique:

- Each test policy dispatches a distinct command type containing its policy name (e.g. `SharedPolicyOneExecuted`, `PartitionedPolicyTwoExecuted`).
- Register one `MessageChannel<TCommand>` singleton per command type.
- Add the generic `MessageChannelHandler<TCommand>` decorator over `ICommandHandler<>`.
- Tests wait on each `MessageChannel<TCommand>` to confirm the matching policy executed.

This keeps the same verification pattern as the current `PolicyListeningTests` and proves the full path from event stream → listener → policy → command handler.

---

## Files to Add / Modify

### New files

- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Environment/AsbIntegrationEnvironmentBase.cs` — abstract base that starts Azurite, Cosmos DB, and the Service Bus emulator, then builds a generic host from an abstract `ConfigureServices` method.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Environment/AllSharedPoliciesEnvironment.cs` — concrete environment for Scenario 1.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Environment/MixedPartitioningEnvironment.cs` — concrete environment for Scenario 2.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Environment/OnlyPartitioningEnvironment.cs` — concrete environment for Scenario 3.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Commands/SharedPolicyOneExecuted.cs` (and `Two`/`Three`) — command records dispatched by shared policies.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Commands/PartitionedPolicyOneExecuted.cs` (and `Two`/`Three`) — command records dispatched by partitioned policies.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/CommandHandlers/PolicyExecutedCommandHandler<TCommand>.cs` — dummy handler for all test commands.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Policies/SharedPolicyOne.cs`, `SharedPolicyTwo.cs`, `SharedPolicyThree.cs` — always-support policies that dispatch their command.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Policies/PartitionedPolicyOne.cs`, `PartitionedPolicyTwo.cs`, `PartitionedPolicyThree.cs` — keyed-partition policies that dispatch their command.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/AllSharedPoliciesTests.cs` — Scenario 1 test class.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/MixedPartitioningTests.cs` — Scenario 2 test class.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/OnlyPartitioningTests.cs` — Scenario 3 test class.
- New collection-definition classes (or replace `TestCollection.cs`) for the three xUnit test collections.

### Modified files

- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Environment/WriteContext.cs` — refactor to inherit from `AsbIntegrationEnvironmentBase` and move only the scenario-specific DI setup into its `ConfigureServices` override. Keep it usable by existing command tests.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/Environment/service-bus-config.json` — add `partitioned-stream` topic + `partitioned-policies` subscription.
- `tests/CascadeEsdm.WriteModel.Tests/FunctionalTests/TestBase.cs` — make generic (`IntegrationTestBase<TEnvironment>`) or add three lightweight test-base classes.

---

## Phase 1: Refactor shared infrastructure into a base environment

Status: Complete

**Phase Summary:** Added `AsbIntegrationEnvironmentBase` (`IAsyncLifetime`) owning the Azurite/Cosmos/Service Bus containers, `ServiceProvider`, `Fixture`, container setup, `SetupEventStream` helpers, `CreateEmulatorClientOptions`, and the generic host build that calls an abstract `ConfigureServices(services, azurite, cosmos, serviceBus)`. `WriteContext` now derives from it and only overrides `ConfigureServices` with the existing command-test composition. Verified: build succeeds; `CommandLifeCycleTests`, `SerialExecutionTests`, `PolicyListeningTests` all pass (5 passed).

### What to change

**`AsbIntegrationEnvironmentBase.cs`**

- Fields / properties:
  - `private readonly AzuriteContainer _azuriteContainer`
  - `private readonly CosmosDbContainer _cosmosContainer`
  - `private readonly ServiceBusContainer _serviceBusContainer`
  - `public IServiceProvider ServiceProvider { get; protected set; }`
  - `public IFixture Fixture { get; }`
- Constructor: build the three containers exactly as `WriteContext` does today; create the AutoFixture with `AutoNSubstituteCustomization`.
- `InitializeAsync()`:
  1. Start all three containers.
  2. Extract Azurite and Cosmos connection strings (Cosmos `http:` → `https:`).
  3. Call `SetupAzurite(azuriteConnectionString)` and `SetupCosmos(cosmosConnectionString)` (move the implementation from `WriteContext`).
  4. Build a generic host: call the abstract `ConfigureServices(IServiceCollection, string azuriteConnectionString, string cosmosConnectionString, string serviceBusConnectionString)`.
  5. `ServiceProvider = app.Services; app.Start();`
- `DisposeAsync()`: dispose Azurite, Cosmos DB, and Service Bus containers.
- Keep `GetAsbConfigPath()` and `SetupEventStream(...)` helpers in the base class (or as `protected` methods).

**`WriteContext.cs`**

- Make `WriteContext : AsbIntegrationEnvironmentBase`.
- Remove all container fields and the `InitializeAsync`/`DisposeAsync`/setup helper implementations (move to base).
- Override `ConfigureServices(...)` and keep the existing command-test composition (executors, appliers, `PersonEatenRemovesPersonPolicy`, unkeyed `AddPolicyListener`, `MessageChannel<>`, `MessageChannelHandler<>` decorator).

### Verification Plan

- `dotnet build tests/CascadeEsdm.WriteModel.Tests/CascadeEsdm.WriteModel.Tests.csproj`
- `dotnet test tests/CascadeEsdm.WriteModel.Tests/CascadeEsdm.WriteModel.Tests.csproj --filter "FullyQualifiedName~CommandLifeCycleTests|FullyQualifiedName~SerialExecutionTests|FullyQualifiedName~PolicyListeningTests"`

Expected: build succeeds; existing functional tests still pass against the refactored environment.

---

## Phase 2: Add test commands, handlers, and policies

Status: Complete

**Phase Summary:** Added six `ICommand` records (`Shared/PartitionedPolicyOne|Two|Three Executed`), a generic `PolicyExecutedCommandHandler<TCommand>` returning an empty `CommandResponse`, and six `IPolicy` classes that always support and dispatch their command via `CommandEnvelope<T>` preserving the event's security context/channel. Build succeeds.

### What to change

**Test command records**

Create six `ICommand` records under `FunctionalTests/Commands/`:
- `SharedPolicyOneExecuted`, `SharedPolicyTwoExecuted`, `SharedPolicyThreeExecuted`
- `PartitionedPolicyOneExecuted`, `PartitionedPolicyTwoExecuted`, `PartitionedPolicyThreeExecuted`

Each carries a `PolicyName` string property set from its type name.

**`PolicyExecutedCommandHandler<TCommand>`**

A single generic handler implementing `ICommandHandler<TCommand>` for all six command types. It returns an empty `CommandResponse` and does not perform domain work.

**`SharedPolicyOne` / `Two` / `Three`**

- Implement `IPolicy`.
- Constructor injects `ICommandHandler<SharedPolicyXExecuted>`.
- `Supports(EventEnvelope) => true`.
- `ExecuteAsync(...)` dispatches its command via `new CommandEnvelope<SharedPolicyXExecuted>(...)`, preserving the event envelope's security context and channel.

**`PartitionedPolicyOne` / `Two` / `Three`**

- Same shape, injecting `ICommandHandler<PartitionedPolicyXExecuted>`.
- Registered under the `"partitioned"` key in the relevant environments.

### Verification Plan

- `dotnet build tests/CascadeEsdm.WriteModel.Tests/CascadeEsdm.WriteModel.Tests.csproj`

Expected: build succeeds; no runtime tests yet.

---

## Phase 3: Add the three concrete environments

Status: Complete

**Phase Summary:** Added `AllSharedPoliciesEnvironment`, `MixedPartitioningEnvironment`, `OnlyPartitioningEnvironment` deriving the base, plus a shared `PolicyTestServiceRegistration.AddPolicyExecutionTracking()` extension that registers the six handlers, the open-generic `MessageChannel<>` singleton, and the `MessageChannelHandler<>` decorator. Collection definitions added in `PartitioningCollections.cs`.

### What to change

For each environment, inherit `AsbIntegrationEnvironmentBase` and override `ConfigureServices(...)` to call `services.AddCascadeEsdm(...)` with the appropriate write-model configuration.

Each environment must also:
- Register each test command/handler: `services.AddScoped<ICommandHandler<TCommand>, PolicyExecutedCommandHandler<TCommand>>()` for all six command types.
- Register a `MessageChannel<TCommand>` singleton for all six command types.
- Add the generic decorator: `services.AddGenericDecorator(typeof(ICommandHandler<>), typeof(MessageChannelHandler<>));`.

**`AllSharedPoliciesEnvironment`**

- `.UsingPolicies(p => p.AddPolicy<SharedPolicyOne>().AddPolicy<SharedPolicyTwo>().AddPolicy<SharedPolicyThree>())`
- `.UsingPolicies("second-stream", p => p.AddPolicy<SharedPolicyOne>().AddPolicy<SharedPolicyTwo>().AddPolicy<SharedPolicyThree>())`
- `.UsingAzureServiceBusReceiver(...)` for unkeyed listener on `example-stream` / `test-policies`.
- `.UsingAzureServiceBusReceiver("second-stream", ...)` for keyed listener on `second-stream` / `second-policies`.
- `.AddPolicyListener()` and `.AddPolicyListener("second-stream")`.

**`MixedPartitioningEnvironment`**

- `.UsingPolicies(p => p.AddPolicy<SharedPolicyOne>().AddPolicy<SharedPolicyTwo>().AddPolicy<SharedPolicyThree>())`
- `.UsingPolicies("partitioned", p => p.AddPolicy<PartitionedPolicyOne>().AddPolicy<PartitionedPolicyTwo>().AddPolicy<PartitionedPolicyThree>())`
- `.UsingAzureServiceBusReceiver(...)` for unkeyed listener on `example-stream` / `test-policies`.
- `.UsingAzureServiceBusReceiver("partitioned", ...)` for keyed listener on `partitioned-stream` / `partitioned-policies`.
- `.AddPolicyListener()` and `.AddPolicyListener("partitioned")`.

**`OnlyPartitioningEnvironment`**

- `.UsingPolicies("partitioned", p => p.AddPolicy<PartitionedPolicyOne>().AddPolicy<PartitionedPolicyTwo>().AddPolicy<PartitionedPolicyThree>())`
- `.UsingAzureServiceBusReceiver("partitioned", ...)` on `partitioned-stream` / `partitioned-policies`.
- `.AddPolicyListener("partitioned")`.

### Collection definitions

Replace `TestCollection.cs` with individual collection definitions, or add new files:

```csharp
[CollectionDefinition("AllSharedPolicies")]
public class AllSharedPoliciesCollection : ICollectionFixture<AllSharedPoliciesEnvironment> { }

[CollectionDefinition("MixedPartitioning")]
public class MixedPartitioningCollection : ICollectionFixture<MixedPartitioningEnvironment> { }

[CollectionDefinition("OnlyPartitioning")]
public class OnlyPartitioningCollection : ICollectionFixture<OnlyPartitioningEnvironment> { }
```

Keep the existing `FunctionalTests` collection definition pointing at `WriteContext` for command tests.

### Verification Plan

- `dotnet build tests/CascadeEsdm.WriteModel.Tests/CascadeEsdm.WriteModel.Tests.csproj`

Expected: build succeeds; host builder wiring compiles.

---

## Phase 4: Update ASB emulator configuration

Status: Complete

**Phase Summary:** Added `second-stream`/`second-policies` and `partitioned-stream`/`partitioned-policies` topics+subscriptions to `service-bus-config.json`.

### What to change

**`service-bus-config.json`**

Add `second-stream` and `partitioned-stream` topics with subscriptions inside the existing namespace. Each mirrors the structure of the existing `example-stream`/`test-policies` topic.

```json
{
  "Name": "second-stream",
  "Properties": {
    "DefaultMessageTimeToLive": "PT1H",
    "DuplicateDetectionHistoryTimeWindow": "PT20S",
    "RequiresDuplicateDetection": false
  },
  "Subscriptions": [
    {
      "Name": "second-policies",
      "Properties": {
        "DeadLetteringOnMessageExpiration": true,
        "DefaultMessageTimeToLive": "PT1H",
        "LockDuration": "PT1M",
        "MaxDeliveryCount": 3,
        "ForwardDeadLetteredMessagesTo": "",
        "ForwardTo": "",
        "RequiresSession": false
      }
    }
  ]
},
{
  "Name": "partitioned-stream",
  "Properties": {
    "DefaultMessageTimeToLive": "PT1H",
    "DuplicateDetectionHistoryTimeWindow": "PT20S",
    "RequiresDuplicateDetection": false
  },
  "Subscriptions": [
    {
      "Name": "partitioned-policies",
      "Properties": {
        "DeadLetteringOnMessageExpiration": true,
        "DefaultMessageTimeToLive": "PT1H",
        "LockDuration": "PT1M",
        "MaxDeliveryCount": 3,
        "ForwardDeadLetteredMessagesTo": "",
        "ForwardTo": "",
        "RequiresSession": false
      }
    }
  ]
}
```

### Verification Plan

- `dotnet test tests/CascadeEsdm.WriteModel.Tests/CascadeEsdm.WriteModel.Tests.csproj --filter "FullyQualifiedName~MixedPartitioningTests"` (after the tests are added)

Expected: tests can connect to the new topic/subscription.

---

## Phase 5: Add the integration test classes

Status: Complete

**Phase Summary:** Added `IntegrationTestBase<TEnvironment>` and the three test classes plus a `PolicyPartitioningTestHelpers` static (send serialized `EventEnvelope`, wait/negative-wait on channels). The `OnlyPartitioning` negative test reuses the `"partitioned"` `ServiceBusClient` to publish to `example-stream` (no unkeyed client exists). Verified: all 6 new tests pass.

### What to change

Make `TestBase` generic or create a new `IntegrationTestBase<TEnvironment>`:

```csharp
[Collection("AllSharedPolicies")] // per concrete test class
public abstract class IntegrationTestBase<TEnvironment> where TEnvironment : AsbIntegrationEnvironmentBase
{
    protected readonly TEnvironment Environment;
    protected readonly ITestOutputHelper Output;

    protected IntegrationTestBase(ITestOutputHelper output, TEnvironment environment)
    {
        Output = output;
        Environment = environment;
    }
}
```

**`AllSharedPoliciesTests`**

- `[Collection("AllSharedPolicies")]`.
- Constructor receives `AllSharedPoliciesEnvironment`.
- `[Fact] public async Task All_Policies_Execute_On_Example_Stream()`
  - Clear all six `MessageChannel<TCommand>` instances.
  - Get the unkeyed `ServiceBusClient` (key `nameof(ServiceBusReceiverBuilder)`).
  - Send a serialised `EventEnvelope` to `example-stream`.
  - Wait on `MessageChannel<SharedPolicyOneExecuted>`, `SharedPolicyTwoExecuted`, `SharedPolicyThreeExecuted`.
  - Assert all three channels receive their command and no partitioned commands arrive.
- `[Fact] public async Task All_Policies_Execute_On_Second_Stream()`
  - Get the keyed `ServiceBusClient` (`"second-stream"`).
  - Send a serialised `EventEnvelope` to `second-stream`.
  - Wait on the three shared `MessageChannel<TCommand>` instances.
  - Assert all three shared policies executed.

**`MixedPartitioningTests`**

- `[Collection("MixedPartitioning")]`.
- `Shared_Policies_Execute_Only_On_Unkeyed_Stream`: send to `example-stream`, wait on the three shared `MessageChannel<TCommand>` instances and assert they receive commands; assert a short timeout elapses without receiving any partitioned commands.
- `Partitioned_Policies_Execute_Only_On_Partitioned_Stream`: send to `partitioned-stream`, wait on the three partitioned `MessageChannel<TCommand>` instances and assert they receive commands; assert no shared commands arrive.

**`OnlyPartitioningTests`**

- `[Collection("OnlyPartitioning")]`.
- `Partitioned_Policies_Execute_On_Partitioned_Stream`: send to `partitioned-stream`, wait on the three partitioned `MessageChannel<TCommand>` instances and assert they receive commands.
- `No_Execution_On_Unkeyed_Stream`: send to `example-stream`, wait on the three partitioned `MessageChannel<TCommand>` instances and assert they time out without receiving any commands (no unkeyed listener is registered, so the message is not consumed by a policy listener).

### Sending messages

Use the keyed `ServiceBusClient` exactly as the existing test does. The service key is the listener name (for unkeyed listeners this is `nameof(ServiceBusReceiverBuilder)`):

```csharp
// unkeyed listener on example-stream
var client = Environment.ServiceProvider.GetRequiredKeyedService<ServiceBusClient>(
    nameof(ServiceBusReceiverBuilder));
var sender = client.CreateSender("example-stream");
await sender.SendMessageAsync(new ServiceBusMessage(payload) { SessionId = envelope.Subject.Value });

// keyed "partitioned" listener on partitioned-stream
var partitionedClient = Environment.ServiceProvider.GetRequiredKeyedService<ServiceBusClient>("partitioned");
var partitionedSender = partitionedClient.CreateSender("partitioned-stream");
await partitionedSender.SendMessageAsync(new ServiceBusMessage(payload) { SessionId = envelope.Subject.Value });

// keyed "second-stream" listener on second-stream
var secondClient = Environment.ServiceProvider.GetRequiredKeyedService<ServiceBusClient>("second-stream");
var secondSender = secondClient.CreateSender("second-stream");
await secondSender.SendMessageAsync(new ServiceBusMessage(payload) { SessionId = envelope.Subject.Value });
```

### Waiting for commands

For each test, resolve the relevant `MessageChannel<TCommand>` instances before sending and call `Clear()` if needed, then call `WaitForNextAsync(TimeSpan.FromSeconds(5))` after sending the message. Use a short timeout for negative assertions.

### Verification Plan

```powershell
dotnet test tests\CascadeEsdm.WriteModel.Tests\CascadeEsdm.WriteModel.Tests.csproj --filter "FullyQualifiedName~AllSharedPoliciesTests|FullyQualifiedName~MixedPartitioningTests|FullyQualifiedName~OnlyPartitioningTests"
```

Expected: all new integration tests pass.

---

## Phase 6: Update `TestBase` / collection wiring for existing tests

Status: Complete

**Phase Summary:** `TestBase` kept as a non-generic `[Collection("FunctionalTests")]` alias of `IntegrationTestBase<WriteContext>`, so existing command/policy tests are unchanged. `TestCollection.cs` still binds `FunctionalTests` to `WriteContext`. Verified: existing functional tests pass.

### What to change

- If `TestBase` was made generic, update existing command tests (`CommandLifeCycleTests`, `SerialExecutionTests`) and the existing `PolicyListeningTests` to inherit from `IntegrationTestBase<WriteContext>` (or keep `TestBase` as a non-generic alias for `IntegrationTestBase<WriteContext>`).
- Ensure `TestCollection.cs` still defines the `FunctionalTests` collection bound to `WriteContext`.
- Run the full functional test suite to confirm no regressions.

### Verification Plan

```powershell
dotnet test tests\CascadeEsdm.WriteModel.Tests\CascadeEsdm.WriteModel.Tests.csproj --filter "FullyQualifiedName~FunctionalTests"
```

Expected: all functional tests pass.

---

## Final Recap

All six phases complete. Shared ASB/container infra moved into `AsbIntegrationEnvironmentBase`; `WriteContext` refactored onto it with no behaviour change. Three concrete environments plus three test classes exercise the keyed policy-partitioning scenarios end-to-end (event stream → keyed/unkeyed listener → keyed/unkeyed policy pool → command handler), verified via `MessageChannel<TCommand>`. `service-bus-config.json` gained `second-stream` and `partitioned-stream` topics.

**Verification result:** WriteModel.Tests build succeeds; the 6 new partitioning tests pass and the 5 existing command/policy functional tests still pass locally against Docker/Testcontainers.

## Deployment Plan

No runtime/infra changes. This work only adds/refactors tests and shared test infrastructure.

1. Merge the PR once the new integration tests and the refactored base environment are green in CI.
2. CI pipeline runs `dotnet test` for the WriteModel.Tests project; the new ASB emulator topics/subscriptions are created automatically from `service-bus-config.json`.
3. Future policy-listener changes can be validated against the three concrete environments.

## Assumptions confirmed

1. **Verification technique**: `MessageChannel<TCommand>` + `MessageChannelHandler<TCommand>`, with each policy dispatching a distinct test command.
2. **Scenario 1 — "two test event streams"**: Two physically separate Service Bus topics (`example-stream` and `second-stream`). The same three shared policies are registered in both the unkeyed pool and a `"second-stream"` keyed pool so that both listeners execute them.
3. **Scenario 3 negative case**: Include a test that sends to `example-stream` and asserts no partitioned commands are received.
