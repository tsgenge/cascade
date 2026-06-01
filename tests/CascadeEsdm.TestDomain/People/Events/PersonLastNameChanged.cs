using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.TestDomain.People.Events;

public record PersonLastNameChanged(Guid Id, string LastName) : IDomainEvent;

internal class PersonLastNameChangedApplier : IEventApplier<PersonLastNameChanged, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonLastNameChanged @event, EventEnvelope envelope)
    {
        if (aggregate.Person != null) {
            aggregate.Person.LastName = new LastName(@event.LastName);
        }
    }
}
