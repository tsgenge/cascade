using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.Abstractions.Domain.ValueObjects;

namespace CascadeEsdm.WriteModel.CommandHandling;

public record CommandResponse : ICommandResponse
{
    public Guid CommandId { get; }
    public string CommandType { get; }
    public ISubject Subject { get; }
    public IReadOnlyList<IEventEnvelope> Events { get; } = new List<IEventEnvelope>();
    public IReadOnlyCollection<AvailableAction> Actions { get; } = new List<AvailableAction>();
    public CommandResponse(ICommandEnvelope cmd, ISubject subject, IReadOnlyList<IEventEnvelope> newEvents)
    {
        CommandId = cmd.Id;
        CommandType = cmd.Type;
        Subject = subject;
        Events = newEvents;
        Actions = new List<AvailableAction>();
    }
    public CommandResponse(ICommandEnvelope cmd, ISubject subject, IReadOnlyList<IEventEnvelope> newEvents, IReadOnlyList<AvailableAction> actions) : this(cmd, subject, newEvents)
    {
        Actions = actions;
    }
}
