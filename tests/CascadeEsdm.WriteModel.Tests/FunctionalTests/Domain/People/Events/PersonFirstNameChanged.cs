using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;

public record PersonFirstNameChanged(Guid Id, string FirstName) : IDomainEvent;

internal class PersonFirstNameChangedApplier : IEventApplier<PersonFirstNameChanged, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonFirstNameChanged @event, IEventEnvelope envelope)
    {
        if (aggregate.Person != null)
        {
            aggregate.Person.FirstName = new (@event.FirstName);
        }
    }
}
