using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Entities;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;

public record PersonAdded(Guid Id, string FirstName, string LastName, string MobileNumber) : IDomainEvent;

internal class PersonAddedApplier : IEventApplier<PersonAdded, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonAdded @event, IEventEnvelope envelope)
    {
        aggregate.Person = new Person(
            new(@event.Id),
            new(@event.FirstName),
            new(@event.LastName),
            new(@event.MobileNumber));
    }
}