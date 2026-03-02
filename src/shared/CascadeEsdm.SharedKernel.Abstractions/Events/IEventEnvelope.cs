using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Events;

public interface IEventEnvelope
{
    Guid Id { get; }
    IEventSource Source { get; }
    ISubject Subject { get; }
    string Type { get; }
    IAuthenticatedContext SecurityContext { get; }
    IClientChannel Channel { get; }
    IDomainEvent Event { get; }
    int Sequence { get; }
    DateTimeOffset Time { get; }
}