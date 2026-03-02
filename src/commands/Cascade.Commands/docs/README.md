# Cascade.Commands

Command handling implementation for the Cascade Event Sourcing framework.

## Overview

This package provides the concrete implementation of command handling infrastructure for event-sourced systems. It includes command bus, handler registration, validation pipeline, and integration with dependency injection.

## Features

- **Command Bus**: In-memory command dispatcher with middleware support
- **Handler Registration**: Automatic discovery and registration of command handlers
- **Validation Pipeline**: Built-in command validation before execution
- **Dependency Injection**: First-class support for Microsoft.Extensions.DependencyInjection
- **Error Handling**: Comprehensive error handling and result types
- **Middleware Support**: Extensible pipeline for cross-cutting concerns

## Installation

```bash
dotnet add package Cascade.Commands
```

This will automatically include dependencies:
- `Cascade.Commands.Abstractions`
- `Cascade.SharedKernel`

## Usage

### Register Command Handlers

```csharp
using Cascade.Commands;

services.AddCascadeCommands(options =>
{
    options.RegisterHandlersFromAssembly(typeof(PlaceOrderHandler).Assembly);
});
```

### Implement a Command Handler

```csharp
public class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand>
{
    private readonly IOrderRepository _repository;
    
    public PlaceOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<CommandResult> HandleAsync(
        PlaceOrderCommand command, 
        CancellationToken cancellationToken)
    {
        var order = new Order();
        order.PlaceOrder(command.OrderId, command.CustomerId);
        
        await _repository.SaveAsync(order, cancellationToken);
        
        return CommandResult.Success();
    }
}
```

### Execute Commands

```csharp
public class OrderController : ControllerBase
{
    private readonly ICommandBus _commandBus;
    
    public OrderController(ICommandBus commandBus)
    {
        _commandBus = commandBus;
    }
    
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request)
    {
        var command = new PlaceOrderCommand(request.OrderId, request.CustomerId);
        var result = await _commandBus.SendAsync(command);
        
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }
}
```

### Add Command Validation

```csharp
public class PlaceOrderValidator : ICommandValidator<PlaceOrderCommand>
{
    public Task<ValidationResult> ValidateAsync(PlaceOrderCommand command)
    {
        if (command.OrderId == null)
            return ValidationResult.Failure("OrderId is required");
            
        if (command.CustomerId == null)
            return ValidationResult.Failure("CustomerId is required");
            
        return ValidationResult.Success();
    }
}
```

## Dependencies

- `Cascade.Commands.Abstractions`
- `Cascade.SharedKernel`
- `Microsoft.Extensions.DependencyInjection.Abstractions`

## Related Packages

- **Cascade.Views**: Query/view implementation for CQRS
- **Cascade.Storage.CosmosDb**: Event store implementation

## Documentation

For complete documentation, examples, and best practices, visit: https://github.com/tsgenge/cascade

## License

BSD 3-Clause License - see LICENSE file for details
