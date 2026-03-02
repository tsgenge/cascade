# Cascade.SharedKernel

Core implementation for the Cascade Event Sourcing framework.

## Overview

This package provides the concrete implementations of the abstractions defined in `Cascade.SharedKernel.Abstractions`. It includes base classes, utilities, and core functionality for building event-sourced domain models.

## Features

- **Aggregate Root Base Classes**: Ready-to-use base classes for your aggregates
- **Event Handling**: Built-in event application and state management
- **Domain Event Infrastructure**: Event metadata, versioning, and serialization support
- **Value Object Utilities**: Helper classes for creating immutable value objects
- **Domain Primitives**: Common domain building blocks

## Installation

```bash
dotnet add package Cascade.SharedKernel
```

This will automatically include `Cascade.SharedKernel.Abstractions` as a dependency.

## Usage

### Creating an Aggregate Root

```csharp
using Cascade.SharedKernel;

public class Order : AggregateRoot
{
    public OrderId Id { get; private set; }
    public OrderStatus Status { get; private set; }
    
    public void PlaceOrder(OrderId id, CustomerId customerId)
    {
        ApplyEvent(new OrderPlaced(id, customerId));
    }
    
    private void Apply(OrderPlaced @event)
    {
        Id = @event.OrderId;
        Status = OrderStatus.Placed;
    }
}
```

### Working with Domain Events

```csharp
public record OrderPlaced(OrderId OrderId, CustomerId CustomerId) : DomainEvent;
```

## Dependencies

- `Cascade.SharedKernel.Abstractions`

## Related Packages

- **Cascade.Commands**: Command handling implementation
- **Cascade.Views**: Query/view implementation
- **Cascade.Storage.CosmosDb**: CosmosDB event store implementation

## Documentation

For complete documentation and examples, visit: https://github.com/tsgenge/cascade

## License

BSD 3-Clause License - see LICENSE file for details
