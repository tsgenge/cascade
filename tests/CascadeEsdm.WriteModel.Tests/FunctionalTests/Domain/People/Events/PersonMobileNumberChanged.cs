using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;

public record PersonMobileNumberChanged(Guid Id, string MobileNumber) : IDomainEvent;

internal class PersonMobileNumberChangedApplier : IEventApplier<PersonMobileNumberChanged, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonMobileNumberChanged @event, IEventEnvelope envelope)
    {
        if (aggregate.Person != null)
        {
            aggregate.Person.MobileNumber = new (@event.MobileNumber);
        }
    }
}
