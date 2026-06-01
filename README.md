# Cascade ESDM

An opinionated C# framework for building **Event Sourced Domain Model** systems — without building the framework yourself.

> *"In the companies I've worked in over the last 10 years I've not seen a single one implement ESDM or even event sourcing. It's just CRUD-based entity obsession and all the quadratic complexity and regret this generates."*
> — [cascade-esdm.org](https://cascade-esdm.org)

---

## The Problem

ESDM is one of the most powerful approaches to system architecture. Events as the source of truth. Commands that express intent. Aggregates that protect invariants. Read models built from facts, not guesses.

The problem is the framework. To implement ESDM correctly you need opinions on dozens of decisions: how commands are dispatched, how aggregates are hydrated, how events are stored, how concurrency is handled, how read models are projected. Every team that tries makes different mistakes, usually without realising they are mistakes until they're baked in.

Cascade removes those decisions. Engineers implement commands, emit events, and build projections. The framework handles everything else.

---

## What's in the Box

### Core packages

| Package | Description |
|---|---|
| `CascadeEsdm.SharedKernel.Abstractions` | Core interfaces — `IDomainEvent`, `IAggregateRoot`, `IEventApplier`, value object contracts |
| `CascadeEsdm.SharedKernel` | Base implementations for aggregates, value objects, and shared kernel types |
| `CascadeEsdm.WriteModel.Abstractions` | Write-side interfaces — `ICommand`, `ICommandExecutor`, `ICommandEnvelope` |
| `CascadeEsdm.WriteModel` | Command dispatch, aggregate hydration, event stream writing, concurrency, MSBuild integration |
| `CascadeEsdm.ReadModel.Abstractions` | Read-side interfaces for projections and queries |
| `CascadeEsdm.ReadModel` | Read model infrastructure |

### Infrastructure packages

| Package | Description |
|---|---|
| `CascadeEsdm.Storage.CosmosDb` | Azure Cosmos DB event stream and read model storage |
| `CascadeEsdm.DistributedLocks` | Azure Storage distributed lock provider for aggregate-level concurrency |
| `CascadeEsdm.Logging.OpenTelemetry` | OpenTelemetry-based structured logging and Application Insights integration |

### Tools

| Package | Description |
|---|---|
| `CascadeEsdm.EventExtractor` | Pre-build tool that extracts `IDomainEvent` records from your write model into a clean, publishable events assembly |

---

## Quick Start

### 1. Install packages

```bash
dotnet add package CascadeEsdm.WriteModel
dotnet add package CascadeEsdm.Storage.CosmosDb
dotnet add package CascadeEsdm.DistributedLocks
dotnet add package CascadeEsdm.Logging.OpenTelemetry
```

### 2. Register in your host

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UseCosmosDbStorage<AppConfig>(storage => storage
            .EventStreamContainer<EventStreamContainer>()
            .WithContainer<ReadModelContainer>())
        .UseAzureDistributedLocks<AppConfig>(config => config.StorageConnectionString)
        .UseApplicationInsights())
    .WithWriteModel(write => write
        .RegisterWriteModel()
        .RegisterCommandsFromAssembly<MyAggregate>()));
```

### 3. Define a command and aggregate

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

### 4. Define an event and apply it

```csharp
public record OrderPlaced(Guid OrderId, string Reference) : IDomainEvent;

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

## Design Principles

**Opinionated by intent.** Cascade has opinions so your engineers don't need to. The right decisions are already made — concurrency strategy, hydration, command dispatch, event storage.

**Technology substitutable.** Azure today, something else tomorrow. Storage and lock providers are pluggable. The domain code doesn't change.

**Engineers focus on function.** Write commands, emit events, build projections. The framework handles the rest.

**Cohesion over fragmentation.** Events and their appliers live together. The extractor publishes only what belongs in the contract. No artificial splits to satisfy infrastructure concerns.

---

## Status

Beta — initial release Q2 2026. The core write model and infrastructure packages are stable. The event extractor is in active development.

Packages are available on [NuGet](https://www.nuget.org/packages?q=cascadeesdm).

---

## Further Reading

- [cascade-esdm.org](https://cascade-esdm.org) — the thinking behind the framework
- [docs/CompositionUsage.md](docs/CompositionUsage.md) — composition and registration patterns
- [docs/EventExtractor.md](docs/EventExtractor.md) — the event extractor in detail

---

## Contributing

Issues and discussions welcome via [GitHub](https://github.com/tsgenge/cascade). Pull requests considered — open an issue first to discuss intent.

---

*Copyright © Tim Genge / Mindfish 2026. BSD-3-Clause.*
