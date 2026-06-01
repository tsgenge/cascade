using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.TestDomain.People.Events;

public record PersonFirstNameChanged(Guid Id, string FirstName) : IDomainEvent;

internal class PersonFirstNameChangedApplier : IEventApplier<PersonFirstNameChanged, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonFirstNameChanged @event, EventEnvelope envelope)
    {
        if (aggregate.Person != null) {
            aggregate.Person.FirstName = new FirstName(@event.FirstName);
        }
    }
}
