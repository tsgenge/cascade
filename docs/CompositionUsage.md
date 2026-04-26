# Cascade ESDM Composition Pattern

## Overview

The Cascade ESDM composition system uses a fluent builder pattern to provide a type-safe, intuitive way to configure your event-sourced application. The pattern enforces that all required infrastructure components are registered before the write model can be configured.

## Basic Usage

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UseCosmosDbStorage<MyAppConfiguration>(storage => storage
            .EventStreamContainer<EventStreamContainer>()
            .WithContainer<ReadModelContainer>())
        .UseAzureDistributedLocks<MyAppConfiguration>(config => config.StorageConnectionString)
        .UseApplicationInsights())
    .WithWriteModel(write => write
        .RegisterWriteModel()
        .RegisterCommandsFromAssembly<MyAggregate>()));
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
infra.UseCosmosDbStorage<TConfig>(storage => storage
    .EventStreamContainer<TEventStreamContainer>()
    .WithContainer<TReadModelContainer>())
```

- `TConfig`: Your application configuration class
- `EventStreamContainer<T>()`: **Required** - Specifies the container for event streams
- `WithContainer<T>()`: Optional - Register additional containers for read models

The `TEventStreamContainer` type must implement `IDocumentContainerDefinition`.

#### Distributed Locks Configuration

```csharp
infra.UseAzureDistributedLocks<TConfig>(config => config.ConnectionString)
```

- `TConfig`: Your application configuration class
- The lambda extracts the Azure Storage connection string from your configuration

#### Telemetry Configuration

```csharp
infra.UseApplicationInsights()
```

Registers OpenTelemetry-based logging with Application Insights.

### 3. Write Model Configuration

```csharp
cascade.WithWriteModel(write => { ... })
```

After infrastructure is configured, you can register the write model components.

#### Register Core Write Model

```csharp
write.RegisterWriteModel()
```

This registers:
- Command authorizers
- Aggregate factories
- Event applier factories
- Event stream readers/writers
- Command handler decorators (logging, event writing, serialization)

#### Register Commands

```csharp
write.RegisterCommandsFromAssembly<TMarker>()
// or
write.RegisterCommandsFromAssembly(assembly)
```

Automatically discovers and registers:
- All aggregates in the assembly
- All commands for those aggregates
- Command handlers
- Command executors

Throws `MissingExecutorException` if any command is missing its executor.

## Configuration Class Example

```csharp
public class MyAppConfiguration
{
    public string StorageConnectionString { get; set; }
    public string DatabaseName { get; set; }
}
```

## Container Definition Example

```csharp
public class EventStreamContainer : IDocumentContainerDefinition
{
    public string Name => "eventstreams";
    public string PartitionKeyPath => "/partitionKey";
}

public class ReadModelContainer : IDocumentContainerDefinition
{
    public string Name => "readmodels";
    public string PartitionKeyPath => "/partitionKey";
}
```

## Validation

The system validates that all required infrastructure components are registered before allowing write model configuration. If any component is missing, an `InvalidOperationException` is thrown with a clear message indicating what's missing:

```
Missing required infrastructure components: Storage Provider, Distributed Lock Provider
```

## Extensibility

### Adding New Storage Providers

Create an extension method on `InfrastructureBuilder`:

```csharp
public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UseTableStorage<TConfig>(
        this InfrastructureBuilder builder,
        Action<TableStorageBuilder<TConfig>> configure)
        where TConfig : class
    {
        var storageBuilder = new TableStorageBuilder<TConfig>(builder);
        configure(storageBuilder);
        
        builder.HasStorage = true;
        
        return builder;
    }
}
```

### Adding New Lock Providers

```csharp
public static InfrastructureBuilder UseRedisLocks<TConfig>(
    this InfrastructureBuilder builder,
    Func<TConfig, string> connectionResolver)
    where TConfig : class
{
    builder.Services.AddSingleton(sp =>
    {
        var config = sp.GetRequiredService<IOptions<TConfig>>();
        var connectionString = connectionResolver(config.Value);
        return new RedisLockProvider(connectionString);
    });
    
    builder.Services.AddTransient<IDistributedLockProvider, RedisDistributedLockProvider>();
    builder.HasLocking = true;
    
    return builder;
}
```

## Benefits

1. **Type Safety**: Generic constraints ensure correct types at compile time
2. **Discoverability**: IntelliSense guides through configuration steps
3. **Validation**: Missing components are caught early with clear error messages
4. **Flexibility**: Easy to plug in different infrastructure implementations
5. **Maintainability**: Clear separation between infrastructure and domain concerns
6. **Cannot Be Done Incorrectly**: The fluent API enforces the correct order and required components
