using Azure.Messaging.ServiceBus;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;

namespace CascadeEsdm.Messaging.AzureServiceBus;

internal abstract class AzureServiceBusReceiverBase : IMessageReceiver
{
    protected Func<Message, CancellationToken, Task>? _handler;

    public abstract Task StartAsync(Func<Message, CancellationToken, Task> handler,
        CancellationToken cancellationToken);

    public abstract Task StopAsync(CancellationToken cancellationToken);

    public async Task ApplyActionAsync(Message message, MessageAction action, Exception? ex,
        CancellationToken cancellationToken)
    {
        await ApplyActionInnerAsync(message, action, ex, cancellationToken);
    }

    protected abstract Task ApplyActionInnerAsync(Message message, MessageAction action, Exception? ex,
        CancellationToken cancellationToken);

    protected static Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        return Task.CompletedTask;
    }
}