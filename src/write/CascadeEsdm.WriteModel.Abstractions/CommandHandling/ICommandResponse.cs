using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.Abstractions.Domain.ValueObjects;

namespace CascadeEsdm.WriteModel.CommandHandling;

public interface ICommandResponse
{
    Guid CommandId { get; }
    string CommandType { get; }
    Subject Subject { get; }
    IReadOnlyList<EventEnvelope> Events { get; }
    IReadOnlyCollection<AvailableAction> Actions { get; }
}