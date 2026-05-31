using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;

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