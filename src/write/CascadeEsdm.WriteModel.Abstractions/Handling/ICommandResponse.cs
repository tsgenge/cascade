using CascadeEsdm.WriteModel.Abstractions.Domain.ValueObjects;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel.Abstractions.Handling;

public interface ICommandResponse
{
    Guid CommandId { get; }
    string CommandType { get; }
    ISubject Subject { get; }
    IReadOnlyList<IEventEnvelope> Events { get; }
    IReadOnlyCollection<AvailableAction> Actions { get; }
}