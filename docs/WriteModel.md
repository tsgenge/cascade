# Write Model - Creating and Configuring Aggregates

## Overview

The write layer is where your domain logic lives. It's responsible for handling commands, validating them, and emitting events that represent the changes to your domain.

## Concepts

| Term | Description |
|---|---|
| **Aggregate** | A cluster of domain objects that are treated as a single unit for data changes |
| **Command** | An instruction to change the state of the domain |
| **Event** | A fact that has happened in the domain |
| **Executor** | The component that handles a command and emits events |
| **Applier** | The component that applies an event to an aggregate |


## Quick Start

Let's focus the Write Model in the quick start.

### 1. Install packages

```bash
dotnet add package CascadeEsdm.WriteModel
dotnet add package CascadeEsdm.Storage.CosmosDb
dotnet add package CascadeEsdm.DistributedLocks
dotnet add package CascadeEsdm.Logging.OpenTelemetry
```

### 2. Create your aggregates
Keep your aggregates small - as small as possible. Its easier to merge aggregates than split them later.

### 3. Create your events
Always start with your events - event storming is a great approach

```csharp
public record OrderPlaced(Guid OrderId, string Reference) : IDomainEvent;
```

### 4. Create your value objects

### 5. Create your Entities

### 6. Create your Event Appliers

``` csharp
/// Recommended within the same file as your Event; cohesion ftw.
internal class OrderPlacedApplier : IEventApplier<OrderPlaced, OrderAggregate>
{
    public void Apply(OrderAggregate aggregate, OrderPlaced @event, EventEnvelope envelope)
    {
        aggregate.OrderId = new(@event.OrderId);
        aggregate.Reference = @event.Reference;
        aggregate.Exists = true;
    }
}
```

### 7. Create your Commands and Executors

```csharp
public record PlaceOrder(OrderId OrderId, string Reference) : ICommand
{
    public Subject GetSubject(ICommandEnvelope envelope) =>
        Subject.ForAggregate<OrderAggregate>(OrderId.Value);
}

internal class PlaceOrderExecutor : ICommandExecutor<PlaceOrder, OrderAggregate>
{
    public async IAsyncEnumerable<EventEnvelope> ExecuteAsync(
        ICommandEnvelope<PlaceOrder> envelope, OrderAggregate aggregate)
    {
        if (aggregate.Exists)
            throw new ConflictException("Order already exists");

        yield return envelope.CreateEvent(
            new OrderPlaced(envelope.Command.OrderId.Value, envelope.Command.Reference),
            aggregate);
    }

    public Task<ISecurityDescriptor?> GetSecurityDescriptorAsync(
        ICommandEnvelope<PlaceOrder> envelope, OrderAggregate aggregate) =>
        Task.FromResult<ISecurityDescriptor?>(null);
}
```

### 8. Register in your host

```csharp
    services.AddCascadeEsdm(o =>
    {
        o.WithInfrastructure(i =>
            {
                i.UsingCosmosDbStorage(cosmosConfig =>
                    {
                        cosmosConfig
                            .WithConnectionString(cosmosConnectionString)
                            .WithOptions(CreateClientOptions())
                            .WithDatabaseName("cascade")
                            .WithEventStreamContainer<EventStreamContainer>();
                    })
                    .UsingApplicationInsights()
                    .UsingAzureDistributedLocks(lb =>
                    {
                        lb.WithConnectionString(azuriteConnectionString);
                    });
            })
            .WithWriteModel(b1 =>
                b1
                    .WithExecutors(h => h
                        .AddCommandExecutor<PlaceOrderExecutor>())
                    .WithAppliers(h => h
                        .AddEventApplier<OrderPlacedApplier>())
            );
    });
```

### 9. Inject and execute commands

```csharp
public class OrdersController : ControllerBase
{
    private readonly ICommandHandler<PlaceOrder> _handler;

    public OrdersController(ICommandHandler<PlaceOrder> handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrder command)
    {
        var result = await _handler.ExecuteAsync(command);
        return Ok(result);
    }
}
```

---

## The Event Extractor

Your event records are the contract between your write model and everything that consumes it — read models, integrations, other services. Cascade includes a pre-build tool that automatically extracts those records into a clean, dependency-light events assembly.

```bash
dotnet tool install -g CascadeEsdm.EventExtractor
```

From your next build, an events project is generated alongside your write model:

```
MyApp.WriteModel/
MyApp.Events/           ← generated, add to source control
  MyApp.Events.csproj
  Orders/
    Events/
      OrderPlaced.cs
```

Write your events and appliers together. Publish only the events. No duplication. No drift. See [docs/EventExtractor.md](docs/EventExtractor.md) for full details.

---