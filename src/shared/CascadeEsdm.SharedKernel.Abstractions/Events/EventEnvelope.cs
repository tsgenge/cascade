using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using System.Text.Json.Serialization;

namespace CascadeEsdm.SharedKernel.Events;

public record EventEnvelope
{
    public EventEnvelope(EventSource source, Subject subject, AuthenticatedContext securityContext,
        ClientChannel channel, IDomainEvent @event, int sequence)
    {
        Event = @event;
        Sequence = sequence;
        Source = source;
        Subject = subject;
        SecurityContext = securityContext;
        Channel = channel;
        Time = DateTimeOffset.UtcNow;
        Type = @event.GetType().Name;
        Id = Guid.NewGuid();
    }

    [JsonConstructor]
    public EventEnvelope(Guid id, EventSource source, Subject subject, AuthenticatedContext securityContext,
        ClientChannel channel, IDomainEvent @event, int sequence, string type, DateTimeOffset time)
    {
        Id = id;
        Subject = subject;
        Event = @event;
        Source = source;
        SecurityContext = securityContext;
        Channel = channel;
        Sequence = sequence;
        Type = type;
        Time = time;
    }

    public Guid Id { get; }
    public EventSource Source { get; }
    public Subject Subject { get; }
    public string Type { get; }
    public AuthenticatedContext SecurityContext { get; }
    public ClientChannel Channel { get; }
    public IDomainEvent Event { get; }
    public int Sequence { get; }
    public DateTimeOffset Time { get; }
}