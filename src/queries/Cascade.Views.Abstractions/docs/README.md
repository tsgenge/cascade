# Cascade.Views.Abstractions

View/Query abstractions for the Cascade Event Sourcing framework.

## Overview

This package provides interfaces and base types for implementing the Query side of CQRS in event-sourced systems. It defines contracts for queries, query handlers, and view models without coupling to specific implementations.

## Key Interfaces

- **IQuery<TResult>**: Marker interface for queries
- **IQueryHandler<TQuery, TResult>**: Interface for query handlers
- **IQueryBus**: Interface for query dispatching
- **IView**: Base interface for read models/views
- **IProjection**: Interface for event projections into views

## Installation

```bash
dotnet add package Cascade.Views.Abstractions
```

## Usage

### Defining a Query

```csharp
using Cascade.Views;

public record GetOrderByIdQuery(OrderId OrderId) : IQuery<OrderView>;
```

### Defining a Query Handler

```csharp
public interface IGetOrderByIdHandler : IQueryHandler<GetOrderByIdQuery, OrderView>
{
    Task<OrderView?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken);
}
```

### Defining a View Model

```csharp
public class OrderView : IView
{
    public string Id { get; set; }
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Defining a Projection

```csharp
public interface IOrderProjection : IProjection<OrderView>
{
    Task ProjectAsync(DomainEvent @event, CancellationToken cancellationToken);
}
```

## Dependencies

None - this package has no external dependencies.

## Related Packages

- **Cascade.Views**: Concrete query handling implementation
- **Cascade.SharedKernel.Abstractions**: Core domain abstractions

## Documentation

For complete documentation and examples, visit: https://github.com/tsgenge/cascade

## License

BSD 3-Clause License - see LICENSE file for details
