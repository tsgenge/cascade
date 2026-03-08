using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;

public record PersonRemoved(Guid Id) : IDomainEvent;

internal class PersonRemovedApplier : IEventApplier<PersonRemoved, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonRemoved @event, IEventEnvelope envelope)
    {
        aggregate.Person = null;
    }
}
