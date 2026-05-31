using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;

public record SecurityDescriptorSet(RoleBasedSecurityDescriptor Descriptor) : IDomainEvent;

internal class SecurityDescriptorApplier : IEventApplier<SecurityDescriptorSet, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, SecurityDescriptorSet @event, EventEnvelope envelope)
    {
        aggregate.SecurityDescriptor = @event.Descriptor;
    }
}