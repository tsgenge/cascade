using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.WriteModel.Security;

public class RoleBasedCommandAuthoriser : ICommandAuthoriser
{
    public Task CanAsync<TCommand>(ICommandEnvelope<TCommand> command, IAccessControlList? accessControlList) where TCommand : ICommand
    {
        //TODO: Need to implement this!
        return Task.CompletedTask;
    }
}