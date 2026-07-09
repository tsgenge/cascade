using Azure.Messaging.ServiceBus;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;

namespace CascadeEsdm.Messaging.AzureServiceBus;

internal class AzureServiceBusSessionReceiver : AzureServiceBusReceiverBase
{
    private readonly ServiceBusSessionProcessor _sessionProcessor;

    public AzureServiceBusSessionReceiver(ServiceBusSessionProcessor sessionProcessor)
    {
        _sessionProcessor = sessionProcessor ?? throw new ArgumentNullException(nameof(sessionProcessor));
    }

    public override Task StartAsync(Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _sessionProcessor.ProcessMessageAsync += OnProcessMessageAsync;
        _sessionProcessor.ProcessErrorAsync += OnProcessErrorAsync;
        _sessionProcessor.SessionClosingAsync += OnSessionClosingAsync;
        return _sessionProcessor.StartProcessingAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return _sessionProcessor.StopProcessingAsync(cancellationToken);
    }

    private async Task OnProcessMessageAsync(ProcessSessionMessageEventArgs args)
    {
        var message = CreateApplicationMessage(args);
        await _handler!(message, args.CancellationToken);
    }

    protected override Task ApplyActionInnerAsync(Message message, MessageAction action, Exception? ex,
        CancellationToken cancellationToken)
    {
        if (message is not AzureServiceBusSessionMessage asbMessage) {
            throw new InvalidOperationException(
                $"Message was not created by {nameof(AzureServiceBusSessionReceiver)}.");
        }

        var eventArgs = asbMessage.EventArgs;

        return action switch
        {
            MessageAction.Complete => eventArgs.CompleteMessageAsync(asbMessage.ReceivedMessage, cancellationToken),
            MessageAction.Abandon => eventArgs.AbandonMessageAsync(asbMessage.ReceivedMessage,
                cancellationToken: cancellationToken),
            MessageAction.DeadLetter => eventArgs.DeadLetterMessageAsync(asbMessage.ReceivedMessage,
                DeadLetterMessageFormatter.GetDeadLetterReason(ex),
                cancellationToken: cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported message action.")
        };
    }

    private static AzureServiceBusSessionMessage CreateApplicationMessage(ProcessSessionMessageEventArgs args)
    {
        var message = args.Message;
        return new AzureServiceBusSessionMessage(
            message.Body.ToString(),
            message.ApplicationProperties,
            message,
            args);
    }

    private static Task OnSessionClosingAsync(ProcessSessionEventArgs args)
    {
        return Task.CompletedTask;
    }
}