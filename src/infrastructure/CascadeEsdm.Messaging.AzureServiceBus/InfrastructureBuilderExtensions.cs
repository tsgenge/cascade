using CascadeEsdm.Messaging.AzureServiceBus;
using CascadeEsdm.SharedKernel.Composition;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UsingAzureServiceBusPolicyListener(
        this InfrastructureBuilder builder,
        Action<ServiceBusReceiverBuilder> configure)
    {
        var receiverBuilder = new ServiceBusReceiverBuilder(builder);
        configure(receiverBuilder);
        receiverBuilder.Build();

        return builder;
    }
}
