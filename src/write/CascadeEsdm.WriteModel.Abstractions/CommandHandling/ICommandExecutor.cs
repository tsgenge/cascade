using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.WriteModel.CommandHandling;


#pragma warning disable CS0618 // Type or member is obsolete
public interface ICommandExecutor<in TCommand, TAggregate> : ICommandExecutor<TAggregate>
#pragma warning restore CS0618 // Type or member is obsolete
    where TCommand : ICommand
    where TAggregate : IAggregateRoot
{
    IAsyncEnumerable<IEventEnvelope> ExecuteAsync(ICommandEnvelope<TCommand> envelope, TAggregate aggregate);
    Task<ISecurityDescriptor?> GetAccessControlListAsync(ICommandEnvelope<TCommand> envelope, TAggregate aggregate);
}

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
[Obsolete("Do not implement this interface directly. Implement ICommandExecutor<TCommand, TAggregate> instead.")]
public interface ICommandExecutor<TAggregate>
    where TAggregate : IAggregateRoot
{ }