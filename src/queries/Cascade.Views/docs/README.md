# Cascade.Views

View/Query implementation for the Cascade Event Sourcing framework.

## Overview

This package provides the concrete implementation of query handling infrastructure for event-sourced systems. It includes query bus, handler registration, projection management, and view materialization support.

## Features

- **Query Bus**: In-memory query dispatcher
- **Handler Registration**: Automatic discovery and registration of query handlers
- **Projection Engine**: Event-driven view materialization
- **View Repositories**: Abstractions for storing and retrieving read models
- **Dependency Injection**: First-class support for Microsoft.Extensions.DependencyInjection
- **Caching Support**: Built-in caching strategies for views

## Installation

```bash
dotnet add package Cascade.Views
```

This will automatically include:
- `Cascade.Views.Abstractions`

## Usage

### Register Query Handlers

```csharp
using Cascade.Views;

services.AddCascadeViews(options =>
{
    options.RegisterHandlersFromAssembly(typeof(GetOrderByIdHandler).Assembly);
    options.RegisterProjectionsFromAssembly(typeof(OrderProjection).Assembly);
});
```

### Implement a Query Handler

```csharp
public class GetOrderByIdHandler : IQueryHandler<GetOrderByIdQuery, OrderView>
{
    private readonly IViewRepository<OrderView> _repository;
    
    public GetOrderByIdHandler(IViewRepository<OrderView> repository)
    {
        _repository = repository;
    }
    
    public async Task<OrderView?> HandleAsync(
        GetOrderByIdQuery query, 
        CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(query.OrderId.ToString(), cancellationToken);
    }
}
```

### Execute Queries

```csharp
public class OrderController : ControllerBase
{
    private readonly IQueryBus _queryBus;
    
    public OrderController(IQueryBus queryBus)
    {
        _queryBus = queryBus;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        var query = new GetOrderByIdQuery(new OrderId(id));
        var view = await _queryBus.QueryAsync(query);
        
        return view != null ? Ok(view) : NotFound();
    }
}
```

### Implement a Projection

```csharp
public class OrderProjection : IProjection<OrderView>
{
    private readonly IViewRepository<OrderView> _repository;
    
    public OrderProjection(IViewRepository<OrderView> repository)
    {
        _repository = repository;
    }
    
    public async Task ProjectAsync(DomainEvent @event, CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case OrderPlaced e:
                await HandleOrderPlaced(e, cancellationToken);
                break;
            case OrderShipped e:
                await HandleOrderShipped(e, cancellationToken);
                break;
        }
    }
    
    private async Task HandleOrderPlaced(OrderPlaced @event, CancellationToken cancellationToken)
    {
        var view = new OrderView
        {
            Id = @event.OrderId.ToString(),
            CustomerId = @event.CustomerId.ToString(),
            Status = "Placed",
            CreatedAt = @event.Timestamp
        };
        
        await _repository.SaveAsync(view, cancellationToken);
    }
    
    private async Task HandleOrderShipped(OrderShipped @event, CancellationToken cancellationToken)
    {
        var view = await _repository.GetByIdAsync(@event.OrderId.ToString(), cancellationToken);
        if (view != null)
        {
            view.Status = "Shipped";
            await _repository.UpdateAsync(view, cancellationToken);
        }
    }
}
```

## Dependencies

- `Cascade.Views.Abstractions`

## Related Packages

- **Cascade.Commands**: Command handling for the write side
- **Cascade.Storage.CosmosDb**: Persistence implementation for views

## Documentation

For complete documentation, examples, and best practices, visit: https://github.com/tsgenge/cascade

## License

BSD 3-Clause License - see LICENSE file for details
