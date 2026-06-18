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

    public EventApplierBuilder AddEventAppliersFromAssembly<TExampleType>()
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

    public EventApplierBuilder AddEventApplier<TApplier>()
        where TApplier : class
    {
        var applierType = typeof(TApplier);
        var applierInterface = applierType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventApplier<,>))
            ?? throw new InvalidOperationException($"{applierType.Name} does not implement IEventApplier<TEvent, TAggregate>.");

        var aggregateType = applierInterface.GetGenericArguments()[1];
        var aggregateApplierInterface = typeof(IEventApplier<>).MakeGenericType(aggregateType);

        _services.AddScoped(applierType);
        _services.AddScoped(applierInterface, sp => sp.GetRequiredService(applierType));
        _services.AddScoped(aggregateApplierInterface, sp => sp.GetRequiredService(applierType));
        return this;
    }
}