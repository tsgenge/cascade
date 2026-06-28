namespace CascadeEsdm.SharedKernel.Infrastructure.Messaging;

public interface IMessageExceptionHandler
{
    Task<MessageAction> HandleAsync(Message message, Exception exception, CancellationToken cancellationToken);
}
