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

    protected string GetDeadLetterReason(Exception? exception)
    {
        if(exception is null)
            return string.Empty;
            
        const int maxLength = 4096;

        var parts = new List<string>();
        var current = exception;
        while (current is not null)
        {
            parts.Add($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }

        var reason = string.Join(" ---> ", parts);

        return reason.Length <= maxLength ? reason : reason[..maxLength];
    }

    protected abstract Task ApplyActionInnerAsync(Message message, MessageAction action, Exception? ex,
        CancellationToken cancellationToken);

    protected static Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        return Task.CompletedTask;
    }
}