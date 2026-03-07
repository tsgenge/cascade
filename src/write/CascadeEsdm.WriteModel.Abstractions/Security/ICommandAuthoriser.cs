using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.WriteModel.Security;

public interface ICommandAuthoriser
{
    Task CanAsync<TCommand>(ICommandEnvelope<TCommand> command, ISecurityDescriptor? accessControlList) where TCommand : ICommand;
}