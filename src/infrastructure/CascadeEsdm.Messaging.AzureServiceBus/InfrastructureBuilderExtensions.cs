using CascadeEsdm.Messaging.AzureServiceBus;
using CascadeEsdm.SharedKernel.Composition;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UsingAzureServiceBusReceiver(
        this InfrastructureBuilder builder,
        Action<ServiceBusReceiverBuilder> configure)
    {
        return builder.UsingAzureServiceBusReceiver(name: null, configure);
    }

    public static InfrastructureBuilder UsingAzureServiceBusReceiver(
        this InfrastructureBuilder builder,
        string? name,
        Action<ServiceBusReceiverBuilder> configure)
    {
        var receiverBuilder = new ServiceBusReceiverBuilder(builder, name);
        configure(receiverBuilder);
        receiverBuilder.Build();

        return builder;
    }
}
