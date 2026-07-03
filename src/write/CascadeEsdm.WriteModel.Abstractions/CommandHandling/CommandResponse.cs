using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.Abstractions.Domain.ValueObjects;

namespace CascadeEsdm.WriteModel.CommandHandling;

public record CommandResponse : ICommandResponse
{
    public CommandResponse(ICommandEnvelope envelope, Subject subject, IReadOnlyList<EventEnvelope> newEvents)
    {
        CommandId = envelope.Id;
        CommandType = envelope.Type;
        Subject = subject;
        Events = newEvents;
        Actions = new List<AvailableAction>();
    }

    public CommandResponse(ICommandEnvelope envelope, IReadOnlyList<EventEnvelope> newEvents)
    {
        CommandId = envelope.Id;
        CommandType = envelope.Type;
        Subject = envelope.Command.GetSubject(envelope);
        Events = newEvents;
        Actions = new List<AvailableAction>();
    }

    public CommandResponse(ICommandEnvelope envelope, Subject subject, IReadOnlyList<EventEnvelope> newEvents,
        IReadOnlyList<AvailableAction> actions) : this(envelope, subject, newEvents)
    {
        Actions = actions;
    }

    public Guid CommandId { get; }
    public string CommandType { get; }
    public Subject Subject { get; }
    public IReadOnlyList<EventEnvelope> Events { get; } = new List<EventEnvelope>();
    public IReadOnlyCollection<AvailableAction> Actions { get; } = new List<AvailableAction>();
}