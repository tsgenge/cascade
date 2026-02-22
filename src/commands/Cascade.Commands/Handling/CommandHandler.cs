using Cascade.Commands.Abstractions.Handling;
using Cascade.Commands.Hydration;
using Cascade.SharedKernel.Aggregates;
using Cascade.SharedKernel.Events;

namespace Cascade.Commands.Handling;

internal abstract class CommandHandler<TCommand, TAggregate, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand
    where TAggregate : IAggregateRoot
    where TResponse : ICommandResponse
{
    private readonly IAggregateHydrator<TAggregate> _hydrator;
    private readonly ICommandExecutorFactory<TAggregate> _executorFactory;
    protected CommandHandler(IAggregateHydrator<TAggregate> hydrator)
    {
        _hydrator = hydrator ?? throw new ArgumentNullException(nameof(hydrator));
    }

    public async Task<TResponse> HandleAsync(ICommandEnvelope<TCommand> commandEnvelope)
    {
        var aggregate = await _hydrator.HydrateAsync(GetSubjectId(commandEnvelope), commandEnvelope.SecurityContext);

        var @events = await ExecuteCommandAsync(commandEnvelope, aggregate);

        return CreateResponse(commandEnvelope, aggregate, events);
    }

    protected virtual async Task<IReadOnlyList<EventEnvelope>> ExecuteCommandAsync(ICommandEnvelope<TCommand> envelope, TAggregate aggregate)
    {
        return await aggregate.ExecuteAsync(envelope);
    }
    protected virtual Guid GetSubjectId(ICommandEnvelope<TCommand> commandEnvelope)
    {
        return commandEnvelope.Command.GetSubject(commandEnvelope).Id;
    }
    
    protected abstract TResponse CreateResponse(ICommandEnvelope<TCommand> commandEnvelope, TAggregate aggregate, IReadOnlyList<IEventEnvelope> @events);
}

internal abstract class CommandHandler<TCommand, TAggregate> : CommandHandler<TCommand, TAggregate, ICommandResponse>, ICommandHandler<TCommand>
    where TCommand : ICommand
    where TAggregate : IAggregateRoot
{
    protected CommandHandler(IAggregateHydrator<TAggregate> hydrator) : base(hydrator)
    {
    }

    protected override ICommandResponse CreateResponse(ICommandEnvelope<TCommand> commandEnvelope, TAggregate aggregate, IReadOnlyList<IEventEnvelope> events)
    {
        return new CommandResponse(commandEnvelope, commandEnvelope.Command.GetSubject(commandEnvelope), @events);
    }
}
