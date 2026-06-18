# Event Conventions

## Standards

- Events are immutable record objects representing historical facts.
- Use primitive types for all event properties (do not use value objects). Events are statements of historical fact — they do not need validation, logic, or transformation that value objects provide.
- All events inherit from the `IDomainEvent` marker interface and are `public`.
- Use primary constructors to enforce value provision at creation.
- Do not include validation or encapsulated logic — events represent truths, not intentions.
- Events do not need to define metadata such as Id, Time, or Subject — these are stored on the `EventEnvelope`.
- Place events in the `Events` folder under their respective aggregate.
- Events are emitted by `ICommandExecutor` implementations during command execution.

```csharp
public record OrderPlaced(Guid OrderId, string Reference) : IDomainEvent;
```

---

## Naming

- Name events in the past tense using a **NounVerb** pattern (e.g. `WorkItemCommentAdded`). They should not include the word "Event".
- Events should be the past tense version of the command, where possible.
- Avoid CRUD verbs (`Created`, `Updated`, `Deleted`), preferring instead `Added`, `Changed`, `Removed`.

---

## Event Envelopes

### Definition

Event envelopes wrap domain events with metadata required for storage, routing, and projection. The `EventEnvelope` is a record in `CascadeEsdm.SharedKernel.Events` that contains the event plus all contextual information about when, why, and by whom the event was created.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier for this event instance |
| `Source` | `EventSource` | References the aggregate type, command ID, and command type that produced this event |
| `Subject` | `Subject` | The aggregate instance this event belongs to |
| `Type` | `string` | The event type name (event.GetType().Name) |
| `SecurityContext` | `AuthenticatedContext` | User identity and tenant that triggered the command |
| `Channel` | `ClientChannel` | Originating channel (used for view projection notifications) |
| `Event` | `IDomainEvent` | The wrapped domain event instance |
| `Sequence` | `int` | Position within the aggregate's event stream |
| `Time` | `DateTimeOffset` | When the event occurred (UTC) |

### Event Source

The `EventSource` value object identifies the origin of an event:

```csharp
// Format: {AssemblyName}/{AggregateType}/{CommandType}/{CommandId}
// Example: MyDomain/OrderAggregate/PlaceOrder/550e8400-e29b-41d4-a716-446655440000
```

Use `EventSource.ForAggregate<TAggregate>(commandId, commandType)` to create an event source for an aggregate.

### Creating Events

Events are created using the `CreateEvent` extension method on `ICommandEnvelope`. This is performed within [Command Executors](Commands.md), out of scope of this document.

```csharp
public async IAsyncEnumerable<IEventEnvelope> ExecuteAsync(
    ICommandEnvelope<PlaceOrder> envelope, OrderAggregate aggregate)
{
    yield return envelope.CreateEvent(
        new OrderPlaced(envelope.Command.OrderId.Value, envelope.Command.Reference),
        aggregate);

    await Task.CompletedTask;
}
```

The extension method automatically:
- Increments `aggregate.LastSequence` for the sequence number
- Creates the `EventSource` from the aggregate type, command ID, and command type
- Extracts the `Subject` from the command
- Copies `SecurityContext` and `Channel` from the command envelope
- Sets `Time` to current UTC
- Sets `Type` to the event type name
- Generates a new `Guid` for the event `Id`

---

## Aggregate Hydration Using Events

- Events are ingested into the aggregate during hydration from the event stream source. This typically occurs during command execution in the `CommandHandler` base and is handled by the framework.
- The `IAggregateHydrator<TAggregate>` implementation forms the aggregate by pulling events from the event stream, resolving the `IEventApplier<TEvent, TAggregate>` for each event, and applying them.

---

## Event Appliers

- Implement `IEventApplier<TEvent, TAggregate>` in the **same file** as the event record. The applier mutates the aggregate directly using its public properties.
- The `IEventApplier` should be implemented as an `internal class`.
- Event appliers do not need to validate the event — it is a historical fact.
- When setting ValueObject properties of an entity during applier execution, use `new()` to reduce `using` statements.
- The `IEventApplier` does not need (and should not) change the `LastSequence` property of the aggregate.
- Event appliers are registered in the composition root via `WithAppliers`.
- Event appliers should be **optimistic** in approach — since they are replaying historical events, they do not need to verify or validate using if statements.

```csharp
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;

public record PersonFirstNameChanged(Guid PersonId, string FirstName) : IDomainEvent;

internal class PersonFirstNameChangedApplier : IEventApplier<PersonFirstNameChanged, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonFirstNameChanged @event, EventEnvelope envelope)
    {
        aggregate.Person.FirstName = new(@event.FirstName);
    }
}
```

The following guard is unnecessary — the event is a historical fact and optimistic application is correct:

```csharp
// ❌ Unnecessary
if (aggregate.Person != null)
{
    aggregate.Person.FirstName = new(@event.FirstName);
}

// ✅ Correct
aggregate.Person.FirstName = new(@event.FirstName);
```

### Composition Root Registration

Event appliers are registered in the composition root via `WithAppliers`. See [CompositionUsage.md](CompositionUsage.md) for full infrastructure setup.

| Method | Description |
|---|---|
| `AddEventApplier<TApplier>()` | Registers a single applier; `TEvent` and `TAggregate` are inferred via reflection from the applier's `IEventApplier<,>` interface |
| `AddEventAppliersFromAssembly<TExampleType>()` | Discovers and registers all `IEventApplier<,>` implementations in the assembly containing `TExampleType` |

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => /* ... */)
    .WithWriteModel(write => write
        .WithAppliers(appliers => appliers
            .AddEventApplier<OrderPlacedApplier>()
            .AddEventApplier<OrderCancelledApplier>())));
```

Or to register all appliers in an assembly at once:

```csharp
write.WithAppliers(appliers => appliers
    .AddEventAppliersFromAssembly<OrderAggregate>())
```

## Inheritance Constraint

The Event Extractor is syntactic — it only extracts records where `IDomainEvent` appears **literally in the record's own base list**. Do not rely on inherited interface satisfaction:

```csharp
// ✅ Extracted — IDomainEvent is in the base list
public record OrderPlaced(Guid OrderId, string Reference) : IDomainEvent;

// ❌ NOT extracted — IDomainEvent is not directly in the base list
public record OrderPlaced(Guid OrderId, string Reference) : OrderEventBase(OrderId);
```

If you want derived records extracted, either keep `IDomainEvent` on each record, or flatten the hierarchy.

---

## Related Conventions

- [Aggregates](Aggregates.md)
- [Commands](Commands.md)
- [Value Objects](ValueObjects.md)
- [Exceptions](Exceptions.md)
- [Event Extractor](EventExtractor.md)
- [Read Model](ReadModel.md) — projecting events into query-optimised views
