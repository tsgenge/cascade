using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Composition;

public class ServiceBusReceiverBuilderTests
{
    [Fact]
    public void ServiceBusReceiverBuilder_WhenConnectionStringMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var infraBuilder = new InfrastructureBuilder(services);

        var act = () => infraBuilder.UsingAzureServiceBusPolicyListener(b =>
        {
            b.WithTopic("my-topic");
            b.WithSubscription("my-subscription");
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Connection string*");
    }

    [Fact]
    public void ServiceBusReceiverBuilder_WhenTopicMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var infraBuilder = new InfrastructureBuilder(services);

        var act = () => infraBuilder.UsingAzureServiceBusPolicyListener(b =>
        {
            b.WithConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=value");
            b.WithSubscription("my-subscription");
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Topic*");
    }

    [Fact]
    public void ServiceBusReceiverBuilder_WhenSubscriptionMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var infraBuilder = new InfrastructureBuilder(services);

        var act = () => infraBuilder.UsingAzureServiceBusPolicyListener(b =>
        {
            b.WithConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=value");
            b.WithTopic("my-topic");
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Subscription*");
    }

    [Fact]
    public void ServiceBusReceiverBuilder_WhenAllSet_RegistersAzureServiceBusReceiverAsIMessageReceiver()
    {
        var services = new ServiceCollection();
        var infraBuilder = new InfrastructureBuilder(services);

        infraBuilder.UsingAzureServiceBusPolicyListener(b =>
        {
            b.WithConnectionString("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=value");
            b.WithTopic("my-topic");
            b.WithSubscription("my-subscription");
        });

        services.Should().Contain(s => s.ServiceType == typeof(IMessageReceiver));
    }
}
