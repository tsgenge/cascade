using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Policies;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Policies;

public class SharedPolicyTwo : IPolicy
{
    private readonly ICommandHandler<SharedPolicyTwoExecuted> _handler;

    public SharedPolicyTwo(ICommandHandler<SharedPolicyTwoExecuted> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public bool Supports(EventEnvelope envelope) => true;

    public async Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        await _handler.HandleAsync(new CommandEnvelope<SharedPolicyTwoExecuted>(
            new SharedPolicyTwoExecuted(),
            envelope.SecurityContext,
            envelope.Channel));
    }
}
