using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.TestDomain.People.Events;

public record PersonRemoved(Guid Id) : IDomainEvent;

internal class PersonRemovedApplier : IEventApplier<PersonRemoved, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonRemoved @event, EventEnvelope envelope)
    {
        aggregate.Person = null;
    }
}
