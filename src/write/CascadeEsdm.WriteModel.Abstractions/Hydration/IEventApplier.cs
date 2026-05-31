using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.WriteModel.Hydration;

public interface IEventApplier<TAggregate>
    where TAggregate : IAggregateRoot { }

public interface IEventApplier<in TEvent, TAggregate> : IEventApplier<TAggregate>
    where TAggregate : IAggregateRoot
    where TEvent : IDomainEvent
{
    void Apply(TAggregate aggregate, TEvent @event, EventEnvelope envelope);
}