# Aggregate Conventions

## Purpose

The aggregate provides the transactional boundary for domain write operations and enforces business rules. Aggregates contain commands which emit events.

Aggregates should be as small as possible; if an aggregate needs information from another to make a decision, consider the architecture of your aggregates — a merge may be required.

---

## Standards

- Aggregates are the root entities of the domain model.
- Aggregates are containers for entities.
- Entities within an aggregate are collections of ValueObjects.
- Entities are mutable.
- Entities are exposed as public properties on the aggregate to allow mutation during event application (Hydration).
- ValueObjects are immutable.
- Aggregates implement `IAggregateRoot`.
- Aggregates are hydrated by replaying events from the event stream — never loaded from a snapshot of their properties.
- State is mutated only inside `IEventApplier.Apply()` — never inside command executors.
- Command executors receive the already-hydrated aggregate; they read state and yield events.

---

## Folder Structure

Aggregates should follow this directory layout within the `/Domain` folder:

```
Domain/
  Orders/                         ← aggregate directory (pluralised)
    OrderAggregate.cs
    Entities/                     ← entity subdirectory
      OrderItem.cs
    ValueObjects/                 ← value object subdirectory
      OrderId.cs
      OrderReference.cs
    Commands/                     ← command subdirectory
      PlaceOrder.cs
    Events/                       ← event subdirectory
      OrderPlaced.cs
    Exceptions/                   ← aggregate-specific exceptions (optional)
      OrderAlreadyExistsException.cs
    Services/                     ← aggregate services (optional)
```

- Each aggregate has its own directory (pluralised) in the `/Domain` folder.
- Subdirectories for Entities, ValueObjects, Commands, and Events are required.
- Subdirectories for Services and Exceptions are optional.

---

## Related Conventions

- [Commands](Commands.md)
- [Events](Events.md)
- [Value Objects](ValueObjects.md)
- [Exceptions](Exceptions.md)
