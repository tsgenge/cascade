using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.TestDomain.People.Entities;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.TestDomain.People.Events;

public record PersonAdded(Guid Id, string FirstName, string LastName, string MobileNumber) : IDomainEvent;

internal class PersonAddedApplier : IEventApplier<PersonAdded, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonAdded @event, EventEnvelope envelope)
    {
        aggregate.Id = @event.Id;

        aggregate.Person = new Person(
            new PersonId(@event.Id),
            new FirstName(@event.FirstName),
            new LastName(@event.LastName),
            new MobileNumber(@event.MobileNumber));
    }
}
