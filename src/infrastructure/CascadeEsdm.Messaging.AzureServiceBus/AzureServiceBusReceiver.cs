using Azure.Messaging.ServiceBus;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;

namespace CascadeEsdm.Messaging.AzureServiceBus;

internal class AzureServiceBusReceiver : IMessageReceiver
{
    private readonly ServiceBusProcessor _processor;
    private Func<Message, CancellationToken, Task>? _handler;

    public AzureServiceBusReceiver(ServiceBusProcessor processor)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

    public Task StartAsync(Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _processor.ProcessMessageAsync += OnProcessMessageAsync;
        _processor.ProcessErrorAsync += OnProcessErrorAsync;
        return _processor.StartProcessingAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _processor.StopProcessingAsync(cancellationToken);
    }

    public Task ApplyActionAsync(Message message, MessageAction action, CancellationToken cancellationToken)
    {
        if (message is not AzureServiceBusMessage asbMessage)
            throw new InvalidOperationException("Message was not created by AzureServiceBusReceiver.");

        var eventArgs = asbMessage.EventArgs;

        return action switch {
            MessageAction.Complete => eventArgs.CompleteMessageAsync(asbMessage.ReceivedMessage, cancellationToken),
            MessageAction.Abandon => eventArgs.AbandonMessageAsync(asbMessage.ReceivedMessage, cancellationToken: cancellationToken),
            MessageAction.DeadLetter => eventArgs.DeadLetterMessageAsync(asbMessage.ReceivedMessage, cancellationToken: cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported message action.")
        };
    }

    private async Task OnProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var applicationProperties = args.Message.ApplicationProperties
            .Where(kvp => kvp.Value is string)
            .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value);

        var message = new AzureServiceBusMessage(
            args.Message.Body.ToString(),
            applicationProperties,
            args.Message,
            args);

        await _handler!.Invoke(message, args.CancellationToken);
    }

    private static Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        return Task.CompletedTask;
    }
}
