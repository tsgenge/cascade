using CascadeEsdm.SharedKernel.Infrastructure.Messaging;

namespace CascadeEsdm.WriteModel.Policies;

internal class DefaultMessageExceptionHandler : IMessageExceptionHandler
{
    public Task<MessageAction> HandleAsync(Message message, Exception exception, CancellationToken cancellationToken)
    {
        return Task.FromResult(MessageAction.DeadLetter);
    }
}
