using Cascade.SharedKernel.Security;
using Cascade.SharedKernel.ValueObjects;

namespace Cascade.SharedKernel.Events;

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