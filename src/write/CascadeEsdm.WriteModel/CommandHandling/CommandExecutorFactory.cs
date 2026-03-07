using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.WriteModel.Exceptions;

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