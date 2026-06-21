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

Register policies using `WithPolicies` on the `WriteModelBuilder`:

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
        .WithPolicies(policies => policies
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
.WithPolicies(policies => policies
    .AddPoliciesFromAssembly<PersonAggregate>())
```

### Namespace Discovery

```csharp
.WithPolicies(policies => policies
    .AddPoliciesFromNamespace<SendWelcomeEmailPolicy>())
```

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
