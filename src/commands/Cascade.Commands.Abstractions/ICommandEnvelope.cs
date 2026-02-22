using Cascade.SharedKernel.Security;
using Cascade.SharedKernel.ValueObjects;

namespace Cascade.Commands;

public interface ICommandEnvelope
{
    Guid Id { get; }
    IAuthenticatedContext SecurityContext { get; }
    IClientChannel Channel { get; }
    DateTimeOffset Time { get; }
    string Type { get; }
    ICommand Command { get; }
}

public interface ICommandEnvelope<out TCommand> : ICommandEnvelope
    where TCommand : ICommand
{
    new TCommand Command { get; }
}