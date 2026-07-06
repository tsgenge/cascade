namespace CascadeEsdm.SharedKernel.Infrastructure.Messaging;

public interface IMessageReceiver
{
    Task StartAsync(Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task ApplyActionAsync(Message message, MessageAction action, Exception? ex, CancellationToken cancellationToken);
}