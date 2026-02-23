using Cascade.SharedKernel.Aggregates;
using Cascade.SharedKernel.Events;

namespace Cascade.Commands.Hydration;

public interface IEventApplier<TAggregate>
    where TAggregate : IAggregateRoot
{ }

public interface IEventApplier<in TEvent, TAggregate> : IEventApplier<TAggregate>
    where TAggregate : IAggregateRoot
    where TEvent : IDomainEvent
{
    void Apply(TAggregate aggregate, TEvent @event, IEventEnvelope envelope);
}