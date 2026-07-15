using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel;
using CascadeEsdm.WriteModel.CommandHandling;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.CommandHandlers;

public class PolicyExecutedCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public Task<ICommandResponse> HandleAsync(ICommandEnvelope<TCommand> envelope)
    {
        return Task.FromResult<ICommandResponse>(
            new CommandResponse(envelope, new List<EventEnvelope>()));
    }
}
