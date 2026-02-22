using Cascade.SharedKernel.Aggregates;
using Cascade.SharedKernel.Events;

namespace Cascade.Commands.Hydration;

internal interface IAggregateFactory
{
    TAggregate GetAggregator<TAggregate>(IEnumerable<IEventEnvelope> events) where TAggregate : IAggregateRoot;
}

internal class AggregateFactory : IAggregateFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AggregateFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TAggregate GetAggregator<TAggregate>(IEnumerable<IEventEnvelope> events)
        where TAggregate : IAggregateRoot
    {
        return ActivatorUtilities.CreateInstance<TAggregate>(_serviceProvider, events);
    }
}