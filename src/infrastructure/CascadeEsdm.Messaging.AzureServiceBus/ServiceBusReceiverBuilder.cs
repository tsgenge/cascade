using Azure.Messaging.ServiceBus;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.Messaging.AzureServiceBus;

public class ServiceBusReceiverBuilder
{
    private readonly InfrastructureBuilder _infraBuilder;
    private string? _connectionString;
    private string? _topic;
    private string? _subscription;

    public ServiceBusReceiverBuilder(InfrastructureBuilder infraBuilder)
    {
        _infraBuilder = infraBuilder ?? throw new ArgumentNullException(nameof(infraBuilder));
    }

    public ServiceBusReceiverBuilder WithConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }

    public ServiceBusReceiverBuilder WithTopic(string topic)
    {
        _topic = topic;
        return this;
    }

    public ServiceBusReceiverBuilder WithSubscription(string subscription)
    {
        _subscription = subscription;
        return this;
    }

    internal void Build()
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Connection string is required. Call WithConnectionString.");

        if (string.IsNullOrEmpty(_topic))
            throw new InvalidOperationException("Topic is required. Call WithTopic.");

        if (string.IsNullOrEmpty(_subscription))
            throw new InvalidOperationException("Subscription is required. Call WithSubscription.");

        _infraBuilder.Services.AddSingleton(_ =>
        {
            var client = new ServiceBusClient(_connectionString);
            return client.CreateProcessor(_topic, _subscription);
        });

        _infraBuilder.Services.AddSingleton<IMessageReceiver, AzureServiceBusReceiver>();
    }
}
