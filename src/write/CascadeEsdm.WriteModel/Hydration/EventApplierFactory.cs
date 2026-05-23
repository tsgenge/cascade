using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.WriteModel.Hydration;

internal interface IEventApplierFactory<TAggregate> where TAggregate : IAggregateRoot
{
    IEventApplier<TEvent, TAggregate> GetFor<TEvent>() where TEvent : IDomainEvent;
}

internal class EventApplierFactory<TAggregate> : IEventApplierFactory<TAggregate> where TAggregate : IAggregateRoot
{
    private readonly IEventApplier<TAggregate>[] _appliers;

    public EventApplierFactory(IEnumerable<IEventApplier<TAggregate>> appliers)
    {
        _appliers = (appliers ?? throw new ArgumentNullException(nameof(appliers))).ToArray();
    }

    public IEventApplier<TEvent, TAggregate> GetFor<TEvent>()
        where TEvent : IDomainEvent
    {
        var executor = _appliers
            .OfType<IEventApplier<TEvent, TAggregate>>()
            .FirstOrDefault();

        if (executor == null)
            throw new UnknownEventException(typeof(TEvent).Name, typeof(TAggregate).Name);

        return executor;
    }
}