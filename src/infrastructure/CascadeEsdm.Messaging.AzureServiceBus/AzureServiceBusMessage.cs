using Azure.Messaging.ServiceBus;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;

namespace CascadeEsdm.Messaging.AzureServiceBus;

internal record AzureServiceBusMessage : Message
{
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

    public ServiceBusReceivedMessage ReceivedMessage { get; }
    public ProcessMessageEventArgs EventArgs { get; }
}

internal record AzureServiceBusSessionMessage : Message
{
    public AzureServiceBusSessionMessage(
        string body,
        IReadOnlyDictionary<string, object> applicationProperties,
        ServiceBusReceivedMessage receivedMessage,
        ProcessSessionMessageEventArgs eventArgs)
        : base(body, applicationProperties)
    {
        ReceivedMessage = receivedMessage;
        EventArgs = eventArgs;
    }

    public ServiceBusReceivedMessage ReceivedMessage { get; }
    public ProcessSessionMessageEventArgs EventArgs { get; }
}