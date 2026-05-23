using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.WriteModel.Exceptions;
#pragma warning disable CS0618 // Type or member is obsolete

namespace CascadeEsdm.WriteModel.CommandHandling;

internal interface ICommandExecutorFactory<TAggregate>
    where TAggregate : IAggregateRoot
{
    ICommandExecutor<TCommand, TAggregate> GetFor<TCommand>()
        where TCommand : ICommand;
}

internal class CommandExecutorFactory<TAggregate> : ICommandExecutorFactory<TAggregate>
    where TAggregate : IAggregateRoot
{
    private readonly ICommandExecutor<TAggregate>[] _executors;

    public CommandExecutorFactory(IEnumerable<ICommandExecutor<TAggregate>> executors)
    {
        _executors = (executors ?? throw new ArgumentNullException(nameof(executors))).ToArray();
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