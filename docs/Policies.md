# Policies

## Overview

Policies react to domain events after they have been persisted. A policy receives an `EventEnvelope`, decides whether it supports that event, and executes side-effects such as issuing further commands, sending notifications, or triggering integrations.

A single event can activate zero or many policies. All supporting policies execute concurrently — successful policies are allowed to complete even if others fail. If any policy fails, a `PolicyExecutionException` is thrown containing details of every failure.

## Concepts

| Term | Description |
|---|---|
| **Policy** | A reactive handler that executes side-effects in response to a domain event |
| **PolicyDispatcher** | Receives an `EventEnvelope`, resolves supporting policies, and executes them |
| **PolicyBuilder** | Fluent builder for registering policies during composition |

## Defining a Policy

Implement `IPolicy` from `CascadeEsdm.WriteModel.Policies`:

```csharp
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Policies;

internal class SendWelcomeEmailPolicy : IPolicy
{
    private readonly IEmailService _emailService;

    public SendWelcomeEmailPolicy(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public bool Supports(EventEnvelope envelope) =>
        envelope.Event is PersonAdded;

    public async Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var @event = (PersonAdded)envelope.Event;
        await _emailService.SendWelcomeAsync(@event.Email, cancellationToken);
    }
}
```

Policies are resolved from DI, so constructor injection works as expected.

## Composition

Register policies using `UsingPolicies` on the `WriteModelBuilder`:

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UsingCosmosDbStorage(storage => storage
            .WithConnectionString(connectionString)
            .WithDatabaseName("cascade")
            .WithEventStreamContainer<EventStreamContainer>())
        .UsingAzureDistributedLocks(locks => locks
            .WithConnectionString(azuriteConnectionString))
        .UsingApplicationInsights())
    .WithWriteModel(write => write
        .UsingExecutors(executors => executors
            .AddCommandsFromAssembly<PersonAggregate>())
        .UsingAppliers(appliers => appliers
            .AddEventAppliersFromAssembly<PersonAggregate>())
        .UsingPolicies(policies => policies
            .AddPolicy<SendWelcomeEmailPolicy>()
            .AddPolicy<NotifyAdminPolicy>())));
```

### Registration Methods

| Method | Description |
|---|---|
| `AddPolicy<TPolicy>()` | Registers a single policy |
| `AddPoliciesFromAssembly<TExampleType>()` | Discovers and registers all `IPolicy` implementations in the assembly containing `TExampleType` |
| `AddPoliciesFromNamespace<TExampleType>()` | Discovers and registers all `IPolicy` implementations in the namespace of `TExampleType` (and child namespaces) |

### Assembly Discovery

```csharp
.UsingPolicies(policies => policies
    .AddPoliciesFromAssembly<PersonAggregate>())
```

### Namespace Discovery

```csharp
.UsingPolicies(policies => policies
    .AddPoliciesFromNamespace<SendWelcomeEmailPolicy>())
```

## Keyed Policy Partitions

By default every policy registered through `UsingPolicies` joins a single shared pool, and every [policy listener](PolicyListener.md) runs every policy. To run an isolated set of policies for a specific listener, register those policies under a key using the keyed `UsingPolicies` overload:

```csharp
.WithWriteModel(write => write
    .UsingExecutors(/* ... */)
    .UsingAppliers(/* ... */)
    .UsingPolicies(policies => policies                 // shared default pool
        .AddPolicy<SharedDefaultPolicy>())
    .UsingPolicies("orders", policies => policies       // isolated to the "orders" partition
        .AddPolicy<OrderPolicy>())
    .UsingPolicies("payments", policies => policies     // isolated to the "payments" partition
        .AddPolicy<PaymentPolicy>())
    .AddPolicyListener()                                // runs the shared default pool
    .AddPolicyListener("orders")                        // runs only OrderPolicy
    .AddPolicyListener("payments"))                     // runs only PaymentPolicy
```

### Partition Semantics

- `UsingPolicies(key, ...)` registers every policy inside the block under `key` (using keyed DI) and registers an `IPolicyDispatcher` keyed with the same value. That dispatcher only ever sees the policies registered under its key.
- `UsingPolicies(...)` (no key) registers policies without a key and registers the unkeyed shared `IPolicyDispatcher`. This is the default pool.
- A keyed [policy listener](PolicyListener.md) (`AddPolicyListener("orders")`) resolves the dispatcher keyed with the same name and therefore runs **only** the policies in that partition.
- An unkeyed listener (`AddPolicyListener()`) resolves the unkeyed dispatcher and runs **only** the shared default policies.
- Multiple `UsingPolicies("sameKey", ...)` calls aggregate into the same partition.
- All discovery methods (`AddPolicy<T>`, `AddPoliciesFromAssembly<T>`, `AddPoliciesFromNamespace<T>`) honour the partition key of the enclosing `UsingPolicies` block.

Existing single-listener/single-pool consumers require no changes — the keyless overload behaves exactly as before.

## Dispatching

Inject `IPolicyDispatcher` wherever events are available and call `DispatchAsync`:

```csharp
public class EventProcessor
{
    private readonly IPolicyDispatcher _policyDispatcher;

    public EventProcessor(IPolicyDispatcher policyDispatcher)
    {
        _policyDispatcher = policyDispatcher;
    }

    public async Task ProcessAsync(EventEnvelope envelope)
    {
        await _policyDispatcher.DispatchAsync(envelope);
    }
}
```

## Error Handling

Policies run concurrently. If one or more policies throw, the dispatcher waits for all remaining policies to complete and then throws a `PolicyExecutionException` containing every failure:

```csharp
try
{
    await _policyDispatcher.DispatchAsync(envelope);
}
catch (PolicyExecutionException ex)
{
    foreach (var failure in ex.Failures)
    {
        logger.LogError(failure.Exception,
            "Policy {PolicyName} failed", failure.PolicyName);
    }
}
```

`PolicyExecutionException.Failures` is an `IReadOnlyList<PolicyFailure>`, where each `PolicyFailure` contains the policy class name and the thrown exception.

## Recommended Folder Structure

```
MyApp.WriteModel/
  People/
    Commands/
    Events/
    Policies/
      SendWelcomeEmailPolicy.cs
      NotifyAdminPolicy.cs
```
