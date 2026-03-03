using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.WriteModel.Handling;

internal interface ICommandExecutorFactory<TAggregate>
    where TAggregate : IAggregateRoot
{
    ICommandExecutor<TCommand, TAggregate> GetFor<TCommand>()
        where TCommand : ICommand;
}

internal interface ICommandExecutor<in TCommand, TAggregate> : ICommandExecutor<TAggregate>
    where TCommand : ICommand
    where TAggregate : IAggregateRoot
{
    IAsyncEnumerable<EventEnvelope> ExecuteAsync(ICommandEnvelope<TCommand> envelope, TAggregate aggregate);
    Task<IAccessControlList?> GetSecurityDescriptorAsync(ICommandEnvelope<TCommand> envelope, TAggregate aggregate);
}

internal interface ICommandExecutor<TAggregate>
    where TAggregate : IAggregateRoot
{ }

internal class CommandExecutorFactory<TAggregate> : ICommandExecutorFactory<TAggregate>
    where TAggregate : IAggregateRoot
{
    private readonly ICommandExecutor<TAggregate>[] _executors;

    public CommandExecutorFactory(ICommandExecutor<TAggregate>[] executors)
    {
        _executors = executors ?? throw new ArgumentNullException(nameof(executors));
    }

    public ICommandExecutor<TCommand, TAggregate> GetFor<TCommand>() where TCommand : ICommand
    {
        var executor = _executors
            .OfType<ICommandExecutor<TCommand, TAggregate>>()
            .FirstOrDefault();

        if (executor == null)
            throw new UnknownCommandException(typeof(TCommand).Name, typeof(TAggregate).Name);

        return executor;
    }
}