using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using System.Text.Json.Serialization;

namespace CascadeEsdm.WriteModel;

public record CommandEnvelope<TCommand> : CommandEnvelope, ICommandEnvelope<TCommand>
    where TCommand : ICommand
{
    [JsonConstructor]
    public CommandEnvelope(Guid id, string type, TCommand command, AuthenticatedContext securityContext,
        ClientChannel channel, DateTimeOffset time) : base(command, securityContext, channel, time)
    {
        Id = id;
        Type = type;
        Command = command;
        Type = typeof(TCommand).Name;
    }

    public CommandEnvelope(TCommand command, AuthenticatedContext securityContext, ClientChannel channel) : base(
        command, securityContext, channel)
    {
        Command = command;
        Type = typeof(TCommand).Name;
    }

    public new TCommand Command { get; }
}

public abstract record CommandEnvelope : ICommandEnvelope
{
    protected CommandEnvelope(ICommand content, AuthenticatedContext context, ClientChannel channel,
        DateTimeOffset time)
    {
        Id = Guid.NewGuid();
        SecurityContext = context;
        Channel = channel;
        Time = time;
        Command = content;
    }

    protected CommandEnvelope(ICommand content, AuthenticatedContext context, ClientChannel channel)
    {
        Id = Guid.NewGuid();
        SecurityContext = context;
        Channel = channel;
        Time = DateTimeOffset.UtcNow;
        Command = content;
    }

    public Guid Id { get; protected set; }
    public AuthenticatedContext SecurityContext { get; }
    public ClientChannel Channel { get; }
    public DateTimeOffset Time { get; }
    public string Type { get; protected set; } = "NotSet";
    public virtual ICommand Command { get; }

    public EventEnvelope CreateEvent<TForAggregate>(IDomainEvent @event, TForAggregate aggregate)
        where TForAggregate : class
    {
        var seqId = 0;
        if (aggregate is IAggregateRoot realAggregate) {
            realAggregate.LastSequence += 1;
            seqId = realAggregate.LastSequence;
        }

        return new EventEnvelope(
            EventSource.ForAggregate<TForAggregate>(Id, Type),
            Command.GetSubject(this),
            SecurityContext,
            Channel,
            @event,
            seqId);
    }

    public EventEnvelope CreateEvent<TForAggregate>(IDomainEvent @event, int seqId = 0)
        where TForAggregate : class
    {
        return new EventEnvelope(
            EventSource.ForAggregate<TForAggregate>(Id, Type),
            Command.GetSubject(this),
            SecurityContext,
            Channel,
            @event,
            seqId);
    }
}