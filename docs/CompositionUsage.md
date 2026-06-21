# Cascade ESDM Composition Pattern

## Overview

The Cascade ESDM composition system uses a fluent builder pattern to provide a type-safe, intuitive way to configure your event-sourced application. The pattern enforces that all required infrastructure components are registered before the write model can be configured.

## Basic Usage

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UsingCosmosDbStorage(storage => storage
            .WithConnectionString(connectionString)
            .WithDatabaseName("cascade")
            .WithEventStreamContainer<EventStreamContainer>())
        .UsingAzureDistributedLocks(locks => locks
            .WithConnectionString(storageConnection))
        .UsingAzureTableStorage(s => s
            .WithConnectionString(storageConnection))            
        .UsingOpenTelemetry())
    .WithWriteModel(write => write
        .UsingExecutors(executors => executors
            .AddCommandExecutor<AddPersonExecutor>()
            .AddCommandExecutor<ChangePersonFirstNameExecutor>())
        .UsingAppliers(appliers => appliers
            .AddEventApplier<PersonAddedApplier>()
            .AddEventApplier<PersonFirstNameChangedApplier>())
        .WithPolicies(policies => policies
            .AddPolicy<SendWelcomeEmailPolicy>()))
    .WithReadModel(read => read
        .WithViews(views => views
            .AddView<PersonView, ViewsContainer>())));
```

## Step-by-Step Breakdown

### 1. Entry Point

```csharp
services.AddCascadeEsdm(cascade => { ... })
```

This is the main entry point that creates a `CascadeBuilder` and allows you to configure the entire system.

### 2. Infrastructure Configuration

```csharp
cascade.WithInfrastructure(infra => { ... })
```

The infrastructure builder requires three components:
- **Storage Provider**: Where events and read models are stored
- **Distributed Lock Provider**: For handling concurrency
- **Telemetry Logger**: For observability

#### Storage Configuration

```csharp
infra.UsingCosmosDbStorage(storage => storage
    .WithConnectionString(connectionString)
    .WithDatabaseName("cascade")
    .WithEventStreamContainer<EventStreamContainer>())
```

- `WithConnectionString(string)`: **Required** - Cosmos DB connection string
- `WithDatabaseName(string)`: **Required** - Database name (defaults to "cascade")
- `WithEventStreamContainer<TContainer>()`: **Required** - Specifies the container for event streams
- `WithOptions(CosmosClientOptions)`: Optional - Configure Cosmos client options

The `TContainer` type must implement `IDocumentContainerDefinition` and have a parameterless constructor.

#### Distributed Locks Configuration

```csharp
infra.UsingAzureDistributedLocks(locks => locks
    .WithConnectionString(connectionString))
```

- `WithConnectionString(string)`: **Required** - Azure Storage connection string for distributed locks

#### Telemetry Configuration

```csharp
infra.UsingApplicationInsights()
```

Registers OpenTelemetry-based logging with Application Insights.

#### SignalR Configuration (Optional)

```csharp
infra.UseSignalR(signalR => signalR
    .ConfigureSignalROptions(options => { ... }))
```

Configures SignalR for real-time view change notifications.

### 3. Write Model Configuration

```csharp
cascade.WithWriteModel(write => { ... })
```

After infrastructure is configured, you can register the write model components.

#### Register Command Executors

```csharp
write.UsingExecutors(executors => executors
    .AddCommandExecutor<TExecutor>()
    .AddCommandExecutor<TExecutor2>())
```

This registers:
- Command authorizers
- Aggregate factories and hydrators
- Event applier factories
- Event stream readers/writers
- Command handler decorators (logging, event writing, serialization)
- The specified command executors

#### Register Event Appliers

```csharp
write.UsingAppliers(appliers => appliers
    .AddEventApplier<TApplier>()
    .AddEventApplier<TApplier2>())
```

Registers the specified event appliers for handling events during aggregate hydration. `TEvent` and `TAggregate` are inferred via reflection from the applier's `IEventApplier<,>` interface.

| Method | Description |
|---|---|
| `AddEventApplier<TApplier>()` | Registers a single applier; `TEvent` and `TAggregate` are inferred via reflection |
| `AddEventAppliersFromAssembly<TExampleType>()` | Discovers and registers all `IEventApplier<,>` implementations in the assembly containing `TExampleType` |

#### Register Policies

```csharp
write.WithPolicies(policies => policies
    .AddPolicy<SendWelcomeEmailPolicy>()
    .AddPoliciesFromAssembly<PersonAggregate>()
    .AddPoliciesFromNamespace<SendWelcomeEmailPolicy>())
```

This registers reactive policies that execute in response to domain events. Policies are resolved from DI and execute concurrently. See [Policies.md](Policies.md) for full details on implementing and dispatching policies.

| Method | Description |
|---|---|
| `AddPolicy<TPolicy>()` | Registers a single policy |
| `AddPoliciesFromAssembly<TExampleType>()` | Discovers and registers all `IPolicy` implementations in the assembly containing `TExampleType` |
| `AddPoliciesFromNamespace<TExampleType>()` | Discovers and registers all `IPolicy` implementations in the namespace of `TExampleType` (and child namespaces) |

### 4. Read Model Configuration

```csharp
cascade.WithReadModel(read => read
    .WithViews(views => views
        .AddView<OrderView, ViewsContainer>()))
```

Registers the read model projections. `WithViews` requires a notification service to have been registered in `WithInfrastructure()` (e.g. via `UseSignalR`).

#### Register Views

| Method | Description |
|---|---|
| `AddView<TView, TContainer>()` | Registers a single view backed by the specified container |
| `AddViewsFromAssembly<TExampleType>(getContainer)` | Discovers all `IView` implementations in the assembly and resolves each container via the provided delegate; throws if any view has no container resolved |

```csharp
read.WithViews(views => views
    .AddView<OrderView, ViewsContainer>()
    .AddView<OrderSummaryView, ViewsContainer>())
```

Or using assembly scanning:

```csharp
read.WithViews(views => views
    .AddViewsFromAssembly<OrderView>(viewType =>
        typeof(ViewsContainer)))
```

## Container Definition Example

```csharp
public class EventStreamContainer : IDocumentContainerDefinition
{
    public string Name => "eventstreams";
    public string PartitionKeyPath => "/partitionKey";
}
```

## Validation

The system validates that all required infrastructure components are registered before allowing write model configuration. If any component is missing, an `InvalidOperationException` is thrown with a clear message indicating what's missing:

```
Missing required infrastructure components: Storage Provider, Distributed Lock Provider, Telemetry Logger, Event Stream Container. Ensure you have called the appropriate Use* methods on the infrastructure builder.
```

## Complete Example

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddCascadeEsdm(cascade => cascade
        .WithInfrastructure(infra => infra
            .UsingCosmosDbStorage(storage => storage
                .WithConnectionString(configuration.GetConnectionString("Cosmos"))
                .WithDatabaseName("myapp")
                .WithEventStreamContainer<EventStreamContainer>()
                .WithOptions(new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ApplicationName = "MyApp"
                }))
            .UsingAzureDistributedLocks(locks => locks
                .WithConnectionString(configuration.GetConnectionString("AzureStorage")))
            .UsingApplicationInsights())
        .WithWriteModel(write => write
            .UsingExecutors(executors => executors
                .AddCommandExecutor<PlaceOrderExecutor>()
                .AddCommandExecutor<CancelOrderExecutor>())
            .UsingAppliers(appliers => appliers
                .AddEventApplier<OrderPlacedApplier>()
                .AddEventApplier<OrderCancelledApplier>())));
}
```

## Extensibility

### Adding New Storage Providers

Create an extension method on `InfrastructureBuilder`:

```csharp
public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UseTableStorage(
        this InfrastructureBuilder builder,
        Action<TableStorageBuilder> configure)
    {
        var storageBuilder = new TableStorageBuilder(builder);
        configure(storageBuilder);
        
        storageBuilder.Build();
        
        return builder;
    }
}
```

### Adding New Lock Providers

```csharp
public static InfrastructureBuilder UseRedisLocks(
    this InfrastructureBuilder builder,
    Action<RedisLockBuilder> configure)
{
    var lockBuilder = new RedisLockBuilder(builder);
    configure(lockBuilder);
    
    lockBuilder.Build();
    
    return builder;
}
```

## Builder Pattern Flow

```
ServiceCollectionExtensions.AddCascadeEsdm()
  └── CascadeBuilder.WithInfrastructure()
        └── InfrastructureBuilder (validates required components)
              ├── ModelBuilder.WithWriteModel()
              │     └── WriteModelBuilder
              │           ├── UsingExecutors() → CommandExecutorBuilder
              │           ├── UsingAppliers() → EventApplierBuilder
              │           └── WithPolicies() → PolicyBuilder
              └── ModelBuilder.WithReadModel()
                    └── ReadModelBuilder
                          └── WithViews() → ViewBuilder
                                ├── AddView<TView, TContainer>()
                                └── AddViewsFromAssembly<TExampleType>(getContainer)
```

## Benefits

1. **Type Safety**: Generic constraints ensure correct types at compile time
2. **Discoverability**: IntelliSense guides through configuration steps
3. **Validation**: Missing components are caught early with clear error messages
4. **Flexibility**: Easy to plug in different infrastructure implementations
5. **Maintainability**: Clear separation between infrastructure and domain concerns
6. **Cannot Be Done Incorrectly**: The fluent API enforces the correct order and required components
