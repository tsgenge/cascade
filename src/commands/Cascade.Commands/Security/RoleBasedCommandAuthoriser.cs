using Cascade.SharedKernel.Security;

namespace Cascade.Commands.Security;

public class RoleBasedCommandAuthoriser : ICommandAuthoriser
{
    public Task CanAsync<TCommand>(ICommandEnvelope<TCommand> command, IAccessControlList? accessControlList) where TCommand : ICommand
    {
        //TODO: Need to implement this!
        return Task.CompletedTask;
    }
}