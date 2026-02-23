using Cascade.SharedKernel.Security;

namespace Cascade.Commands.Security;

public interface ICommandAuthoriser
{
    Task CanAsync<TCommand>(ICommandEnvelope<TCommand> command, IAccessControlList? accessControlList) where TCommand : ICommand;
}