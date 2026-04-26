using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;
using Microsoft.Extensions.DependencyInjection;

public class EventApplierBuilder
{
    private readonly IServiceCollection _services;
    
    public EventApplierBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public EventApplierBuilder RegisterEventAppliersFromAssembly<TExampleType>()
    {
        var targetAssembly = typeof(TExampleType).Assembly;
        var assemblyTypes = targetAssembly.GetTypes();
        var eventApplierTypes = assemblyTypes.Where(t => t.IsClass && !t.IsAbstract && typeof(IEventApplier<,>).IsAssignableFrom(t));

        foreach (var eventApplierType in eventApplierTypes)
        {
            _services.AddScoped(eventApplierType);
        }

        return this;
    }

    public EventApplierBuilder RegisterEventApplier<TEvent, TAggregate, TApplier>() 
        where TApplier : class, IEventApplier<TEvent, TAggregate>
        where TEvent : IDomainEvent
        where TAggregate : IAggregateRoot
    {
        _services.AddScoped<IEventApplier<TEvent, TAggregate>, TApplier>();
        return this;
    }
}
