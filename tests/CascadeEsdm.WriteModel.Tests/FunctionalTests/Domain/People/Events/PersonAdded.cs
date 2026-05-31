using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Entities;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;

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

internal class PersonAddedApplier2 : IEventApplier<PersonAdded, PersonAggregate>
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