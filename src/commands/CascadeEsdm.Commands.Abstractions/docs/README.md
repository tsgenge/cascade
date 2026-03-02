# Cascade.Commands.Abstractions

Command abstractions for the Cascade Event Sourcing framework.

## Overview

This package provides interfaces and base types for implementing the Command pattern in event-sourced systems. It defines contracts for command handlers, command validation, and command execution without coupling to specific implementations.

## Key Interfaces

- **ICommand**: Marker interface for commands
- **ICommandHandler<TCommand>**: Interface for command handlers
- **ICommandValidator<TCommand>**: Interface for command validation
- **ICommandBus**: Interface for command dispatching
- **Command Results**: Abstractions for command execution results

## Installation

```bash
dotnet add package Cascade.Commands.Abstractions
```

## Usage

### Defining a Command

```csharp
using Cascade.Commands;

public record PlaceOrderCommand(OrderId OrderId, CustomerId CustomerId) : ICommand;
```

### Defining a Command Handler

```csharp
public interface IPlaceOrderHandler : ICommandHandler<PlaceOrderCommand>
{
    Task<CommandResult> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken);
}
```

### Command Validation

```csharp
public interface IPlaceOrderValidator : ICommandValidator<PlaceOrderCommand>
{
    Task<ValidationResult> ValidateAsync(PlaceOrderCommand command);
}
```

## Dependencies

- `Cascade.SharedKernel.Abstractions`

## Related Packages

- **Cascade.Commands**: Concrete command handling implementation
- **Cascade.SharedKernel**: Core domain model support

## Documentation

For complete documentation and examples, visit: https://github.com/tsgenge/cascade

## License

BSD 3-Clause License - see LICENSE file for details
