using Cascade.Commands.Abstractions.Domain.ValueObjects;
using Cascade.SharedKernel.Events;
using Cascade.SharedKernel.ValueObjects;

namespace Cascade.Commands.Abstractions.Handling;

public interface ICommandResponse
{
    Guid CommandId { get; }
    string CommandType { get; }
    ISubject Subject { get; }
    IReadOnlyList<IEventEnvelope> Events { get; }
    IReadOnlyCollection<AvailableAction> Actions { get; }
}