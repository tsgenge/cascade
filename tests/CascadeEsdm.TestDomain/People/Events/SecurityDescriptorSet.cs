using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.TestDomain.People.Events;

public record SecurityDescriptorSet(MySecurityDescriptor Descriptor) : IDomainEvent;

internal class SecurityDescriptorApplier : IEventApplier<SecurityDescriptorSet, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, SecurityDescriptorSet @event, EventEnvelope envelope)
    {
        aggregate.SecurityDescriptor = @event.Descriptor;
    }
}