using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Composition;

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
        var eventApplierTypes = assemblyTypes
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventApplier<,>)));

        foreach (var eventApplierType in eventApplierTypes) {
            var eventApplierInterface = eventApplierType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventApplier<,>));

            var aggregateType = eventApplierInterface.GetGenericArguments()[1];
            var aggregateApplierInterface = typeof(IEventApplier<>).MakeGenericType(aggregateType);

            _services.AddScoped(eventApplierInterface, eventApplierType);
            _services.AddScoped(aggregateApplierInterface, sp => sp.GetRequiredService(eventApplierType));
        }

        return this;
    }

    public EventApplierBuilder RegisterEventApplier<TEvent, TApplier, TAggregate>()
        where TApplier : class, IEventApplier<TEvent, TAggregate>
        where TEvent : IDomainEvent
        where TAggregate : IAggregateRoot
    {
        _services.AddScoped<TApplier>();
        _services.AddScoped<IEventApplier<TEvent, TAggregate>, TApplier>(sp => sp.GetRequiredService<TApplier>());
        _services.AddScoped<IEventApplier<TAggregate>, TApplier>(sp => sp.GetRequiredService<TApplier>());
        return this;
    }
}