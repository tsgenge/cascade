using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.Commands.Security;

public interface ICommandAuthoriser
{
    Task CanAsync<TCommand>(ICommandEnvelope<TCommand> command, IAccessControlList? accessControlList) where TCommand : ICommand;
}