---
trigger: glob
globs: **/Commands/*
---

## Standards
- Commands are immutable record objects with primary constructors accepting only value objects (valueobject-conventions.md), ensuring validity.
- Commands should not be shared across aggregates. Some commands may feature the same name (for example SetSecurityDescriptor) but all aggregates should have their own implementation to prevent dependencies.
- If shared services need to recognise shared commands, use a shared interface in a common library (Shared Kernel).
- Place commands in the `Commands` folder within the aggregate directory.
- A command cannot exist without being valid due to the role of Value Objects in validation.
- Commands implement `ICommand`, forcing implementation of GetSubject method.
- Commands are created public.
- Commands that are not "Add" commands should feature the ID of the aggregate as a property to allow formation of the Subject. This should (as all properties on commands) be a value object.
- Use the static factory methods of Subject in the GetSubject for convenience. Since a command is always per aggregate, it always knows what aggregate it is for.

```csharp
public record MyCommand(string Name) : ICommand
{
    public Subject GetSubject() => Subject.For<MyAggregate>(Name);
}
```

## Naming
- Commands should be named in the imperative as VerbNoun. 
- Avoid CRUD terminology (i.e. Create, Update, Delete, etc.), preferring instead Add, Change and Remove.
- Commands don't need to have Time, Id and other metadata - these are instead on the ICommandEnvelope.
- Commands should where possible be named uniquely to prevent confusion in the domain. For example; rather than ChangeName use ChangePersonName to make it explicit.

## Command Executor
- The framework handles marshalling of the command handler via the ICommandExecutor<TCommand, TAggregate> implementation.
- Each command has a single `ICommandExecutor<TCommand, TAggregate>`, implemented in the same file to ensure a high 'topological cohesion'.
- The `ICommandExecutor` validates the command and emits one or more events representing performed mutations. It should be implemented as an internal class.
- The `ICommandExecutor` must implement both `ExecuteAsync` and `GetSecurityDescriptorAsync` methods.
- `ExecuteAsync` should emit events based on the command without altering the aggregate state directly.
- `ExecuteAsync` should await a Task.Complete if no actual asynchronous activity is taken place, with the signature of the method always using the `async` keyword. For example;

``` csharp
    public async IAsyncEnumerable<IEventEnvelope> ExecuteAsync(ICommandEnvelope<MyCommand> envelope, MyAggregate aggregate)
    {
        // Validate Aggregate state
        
        // Emit events using yield return

        // Await a completed task to satisfy the async requirement
        await Task.CompletedTask;
    }
```

- Validation errors should throw suitable exceptions on failure; these can be found in src\write\CascadeEsdm.WriteModel.Abstractions\Exceptions. New exceptions can be created where needed, and placed into the aggregates /Exceptions directory. Exceptions must inherit from src\shared\CascadeEsdm.SharedKernel.Abstractions\Exceptions\ExceptionBase.cs.
- Commands should not directly change the aggregate state - they should emit events that the aggregate will apply.
- Exceptions thrown should inherit from ExceptionBase and have a suitable message and have a suitable HttpStatus code defined (or inherit from an exception where this is already set). See exception-conventions.md for more details.
- Events being emitted should use the ICommandEnvelope extension method available here (src\write\CascadeEsdm.WriteModel.Abstractions\CommandHandling\CommandExtensions.cs) to create the event efficiently.
- Events can emit multiple events by using `yield return`.
- `GetSecurityDescriptorAsync` provides the security context for the command execution.
- The ICommandExecutor for the command will be discovered and registered in the Composition Root. The base interface of ICommandExecutor<TAggregate> is used for resolution in the CommandExecutorFactory, and should not be directly implemented (we use obselete messaging to protect against this softly).