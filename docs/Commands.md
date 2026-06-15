# Command Conventions

## Standards

- Commands are immutable record objects with primary constructors accepting only value objects (see [ValueObjects.md](ValueObjects.md)), ensuring validity.
- Commands should not be shared across aggregates. Some commands may share a name (for example `SetSecurityDescriptor`) but each aggregate should have its own implementation to prevent coupling.
- If shared services need to recognise shared commands, use a shared interface in a common library (Shared Kernel).
- Place commands in the `Commands` folder within the aggregate directory.
- A command cannot exist without being valid due to the role of Value Objects in validation. This removes a huge amount of "logic" checking for validity in your domain.
- Commands implement `ICommand`, which requires a `GetSubject` method.
- Commands are created `public`.
- Commands that are not "Add" commands should include the ID of the aggregate as a property (as a value object) to allow formation of the Subject.
- Use the static factory methods of `Subject` in `GetSubject` for convenience. Since a command is always per aggregate, it always knows what aggregate it is for.

```csharp
public record PlaceOrder(OrderId OrderId, string Reference) : ICommand
{
    public Subject GetSubject(ICommandEnvelope envelope) =>
        Subject.ForAggregate<OrderAggregate>(OrderId.Value);
}
```

---

## Naming

- Commands should be named in the imperative as **VerbNoun**.
- Avoid CRUD terminology (`Create`, `Update`, `Delete`), preferring instead `Add`, `Change`, and `Remove`.
- Commands don't need Time, Id, or other metadata — these are on the `ICommandEnvelope`.
- Commands should where possible be named uniquely to prevent confusion in the domain. For example, use `ChangePersonName` rather than `ChangeName` to make it explicit.

---

## Command Executor

- The framework handles marshalling of the command handler via the `ICommandExecutor<TCommand, TAggregate>` implementation.
- Each command has a single `ICommandExecutor<TCommand, TAggregate>`, implemented in the **same file** to ensure high topological cohesion.
- The `ICommandExecutor` validates the command and emits one or more events representing performed mutations. It should be implemented as an `internal class`.
- The `ICommandExecutor` must implement both `ExecuteAsync` and `GetSecurityDescriptorAsync` methods.
- `ExecuteAsync` should emit events based on the command without altering the aggregate state directly.
- `ExecuteAsync` should `await Task.CompletedTask` if no actual asynchronous activity takes place, with the method signature always using the `async` keyword:

```csharp
public async IAsyncEnumerable<IEventEnvelope> ExecuteAsync(
    ICommandEnvelope<PlaceOrder> envelope, OrderAggregate aggregate)
{
    if (aggregate.Exists)
        throw new ConflictException("Order already exists");

    yield return envelope.CreateEvent(
        new OrderPlaced(envelope.Command.OrderId.Value, envelope.Command.Reference),
        aggregate);

    await Task.CompletedTask;
}
```

- Validation errors should throw suitable exceptions on failure; common exceptions are in `CascadeEsdm.WriteModel.Abstractions/Exceptions`. New exceptions can be created where needed and placed into the aggregate's `/Exceptions` directory. Exceptions must inherit from `ExceptionBase` (see [Exceptions.md](Exceptions.md)).
- Commands should not directly change the aggregate state — they should emit events that the aggregate will apply.
- Events are emitted using the `ICommandEnvelope` extension method from `CascadeEsdm.WriteModel.Abstractions/CommandHandling/CommandExtensions.cs`.
- Multiple events can be emitted by using `yield return`.
- `GetSecurityDescriptorAsync` provides the security context for the command execution.
- The `ICommandExecutor` for each command is discovered and registered automatically in the Composition Root.

---

## Command Envelopes

### Definition

Command envelopes wrap commands with metadata required for processing. The framework provides `ICommandEnvelope` and `ICommandEnvelope<TCommand>` interfaces, with `CommandEnvelope` and `CommandEnvelope<TCommand>` implementations. `ICommandEnvelope` should be avoided - it's used for serialisation purposes and is marked as deprecated.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier for this command invocation |
| `Type` | `string` | The command type name (typeof(TCommand).Name) |
| `Command` | `ICommand` / `TCommand` | The wrapped command instance |
| `SecurityContext` | `AuthenticatedContext` | User identity and tenant information |
| `Channel` | `ClientChannel` | Originating channel (e.g., API, WebSocket) |
| `Time` | `DateTimeOffset` | When the command was created (UTC) |

### Instancing

Create a `CommandEnvelope<TCommand>` using the constructor:

```csharp
var envelope = new CommandEnvelope<PlaceOrder>(
    command: new PlaceOrder(orderId, reference),
    securityContext: new AuthenticatedContext(userIdentity, tenant),
    channel: new ClientChannel("api"));
```

The envelope automatically assigns:
- A new `Guid` for `Id`
- The current UTC time for `Time`
- The command type name for `Type`

For serialization scenarios, a constructor accepting all properties is also available.

---
### Client Channel
The client channel is used during the asynchronous eventual consistency. When an event is used to project an view, on its update the source event ClientChannel is used to notify the client of the update. Clients generally use the update of a view to refresh their local state.

---

## Concurrency Locking

Apply `[CommandLock]` to a command to acquire a distributed lock before execution. The lock can be scoped to the subject (aggregate-level) or to the subject + command type (command-level).

---

## Related Conventions

- [Aggregates](Aggregates.md)
- [Events](Events.md)
- [Value Objects](ValueObjects.md)
- [Exceptions](Exceptions.md)
