using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.TestDomain.People.Events;

public record PersonMobileNumberChanged(Guid Id, string MobileNumber) : IDomainEvent;

internal class PersonMobileNumberChangedApplier : IEventApplier<PersonMobileNumberChanged, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonMobileNumberChanged @event, EventEnvelope envelope)
    {
        if (aggregate.Person != null) {
            aggregate.Person.MobileNumber = new MobileNumber(@event.MobileNumber);
        }
    }
}
