using Azure.Messaging.ServiceBus;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;

namespace CascadeEsdm.Messaging.AzureServiceBus;

internal record AzureServiceBusMessage : Message
{
    public ServiceBusReceivedMessage ReceivedMessage { get; }
    public ProcessMessageEventArgs EventArgs { get; }

    public AzureServiceBusMessage(
        string body,
        IReadOnlyDictionary<string, object> applicationProperties,
        ServiceBusReceivedMessage receivedMessage,
        ProcessMessageEventArgs eventArgs)
        : base(body, applicationProperties)
    {
        ReceivedMessage = receivedMessage;
        EventArgs = eventArgs;
    }
}
