using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.Abstractions.Domain.ValueObjects;

namespace CascadeEsdm.WriteModel.CommandHandling;

public record CommandResponse : ICommandResponse
{
    public CommandResponse(ICommandEnvelope cmd, Subject subject, IReadOnlyList<EventEnvelope> newEvents)
    {
        CommandId = cmd.Id;
        CommandType = cmd.Type;
        Subject = subject;
        Events = newEvents;
        Actions = new List<AvailableAction>();
    }

    public CommandResponse(ICommandEnvelope cmd, Subject subject, IReadOnlyList<EventEnvelope> newEvents,
        IReadOnlyList<AvailableAction> actions) : this(cmd, subject, newEvents)
    {
        Actions = actions;
    }

    public Guid CommandId { get; }
    public string CommandType { get; }
    public Subject Subject { get; }
    public IReadOnlyList<EventEnvelope> Events { get; } = new List<EventEnvelope>();
    public IReadOnlyCollection<AvailableAction> Actions { get; } = new List<AvailableAction>();
}