using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Exceptions;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Security;

namespace CascadeEsdm.WriteModel.CommandHandling;

internal abstract class CommandHandler<TCommand, TAggregate, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand
    where TAggregate : IAggregateRoot
    where TResponse : ICommandResponse
{
    private readonly IAggregateHydrator<TAggregate> _hydrator;
    private readonly ICommandAuthoriser _authoriser;
    private readonly ICommandExecutorFactory<TAggregate> _executorFactory;

    protected CommandHandler(IAggregateHydrator<TAggregate> hydrator, ICommandAuthoriser authoriser,
        ICommandExecutorFactory<TAggregate> executorFactory)
    {
        _hydrator = hydrator ?? throw new ArgumentNullException(nameof(hydrator));
        _authoriser = authoriser ?? throw new ArgumentNullException(nameof(authoriser));
        _executorFactory = executorFactory ?? throw new ArgumentNullException(nameof(executorFactory));
    }

    public async Task<TResponse> HandleAsync(ICommandEnvelope<TCommand> commandEnvelope)
    {
        var aggregate = await _hydrator.HydrateAsync(GetSubjectId(commandEnvelope), commandEnvelope.SecurityContext);

        var @events = await ExecuteCommandAsync(commandEnvelope, aggregate);

        return CreateResponse(commandEnvelope, aggregate, events);
    }

    protected virtual async Task<IReadOnlyList<IEventEnvelope>> ExecuteCommandAsync(ICommandEnvelope<TCommand> envelope, TAggregate aggregate)
    {
        var executor = _executorFactory.GetFor<TCommand>();

        await _authoriser.CanAsync(envelope,
            await executor.GetSecurityDescriptorAsync(envelope, aggregate));

        var events = new List<IEventEnvelope>();
        try
        {
            var eventFeed = executor.ExecuteAsync(envelope, aggregate);

            await foreach (var evt in eventFeed)
            {
                events.Add(evt);
            }
        }
        catch (ExceptionBase)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CommandProcessingException(ex);
        }

        return events;
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
    protected CommandHandler(IAggregateHydrator<TAggregate> hydrator, ICommandAuthoriser authoriser, ICommandExecutorFactory<TAggregate> executorFactory) : base(hydrator, authoriser, executorFactory)
    {
    }

    protected override ICommandResponse CreateResponse(ICommandEnvelope<TCommand> commandEnvelope, TAggregate aggregate, IReadOnlyList<IEventEnvelope> events)
    {
        return new CommandResponse(commandEnvelope, commandEnvelope.Command.GetSubject(commandEnvelope), @events);
    }
}
