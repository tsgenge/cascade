using Azure.Messaging.ServiceBus;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.Messaging.AzureServiceBus;

public class ServiceBusReceiverBuilder
{
    private readonly InfrastructureBuilder _infraBuilder;
    private readonly string? _name;
    private string? _connectionString;
    private TimeSpan? _maxAutoLockRenewalDuration;
    private bool _requiresSessions;
    private TimeSpan? _sessionIdleTimeout;
    private string? _subscription;
    private string? _topic;

    internal ServiceBusReceiverBuilder(InfrastructureBuilder infraBuilder, string? name = null)
    {
        _infraBuilder = infraBuilder ?? throw new ArgumentNullException(nameof(infraBuilder));
        _name = name ?? nameof(ServiceBusReceiverBuilder);
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

    public ServiceBusReceiverBuilder WithSessions(bool requiresSessions = true)
    {
        _requiresSessions = requiresSessions;
        return this;
    }

    public ServiceBusReceiverBuilder WithSessionIdleTimeout(TimeSpan timeout)
    {
        _sessionIdleTimeout = timeout;
        return this;
    }

    public ServiceBusReceiverBuilder WithMaxAutoLockRenewalDuration(TimeSpan duration)
    {
        _maxAutoLockRenewalDuration = duration;
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

        _infraBuilder.Services.AddKeyedSingleton<ServiceBusClient>(_name,
            (_, _) => new ServiceBusClient(_connectionString));

        if (_requiresSessions) {
            var sessionOptions = new ServiceBusSessionProcessorOptions
            {
                SessionIdleTimeout = _sessionIdleTimeout ?? TimeSpan.MaxValue,
                MaxAutoLockRenewalDuration = _maxAutoLockRenewalDuration ?? TimeSpan.FromMinutes(5)
            };

            _infraBuilder.Services.AddKeyedSingleton<ServiceBusSessionProcessor>(_name, (sp, _) =>
            {
                var client = sp.GetRequiredKeyedService<ServiceBusClient>(_name);
                return client.CreateSessionProcessor(_topic, _subscription, sessionOptions);
            });

            if (_name == nameof(ServiceBusReceiverBuilder)) {
                _infraBuilder.Services.AddSingleton<IMessageReceiver>(sp =>
                    new AzureServiceBusSessionReceiver(sp.GetRequiredKeyedService<ServiceBusSessionProcessor>(_name)));
            }
            else {
                _infraBuilder.Services.AddKeyedSingleton<IMessageReceiver>(_name, (sp, key) =>
                    new AzureServiceBusSessionReceiver(sp.GetRequiredKeyedService<ServiceBusSessionProcessor>(key)));
            }
        }
        else {
            _infraBuilder.Services.AddKeyedSingleton<ServiceBusProcessor>(_name, (sp, _) =>
            {
                var client = sp.GetRequiredKeyedService<ServiceBusClient>(_name);
                return client.CreateProcessor(_topic, _subscription, new ServiceBusProcessorOptions());
            });

            if (_name == nameof(ServiceBusReceiverBuilder)) {
                _infraBuilder.Services.AddSingleton<IMessageReceiver>(sp =>
                    new AzureServiceBusReceiver(sp.GetRequiredKeyedService<ServiceBusProcessor>(_name)));
            }
            else {
                _infraBuilder.Services.AddKeyedSingleton<IMessageReceiver>(_name, (sp, key) =>
                    new AzureServiceBusReceiver(sp.GetRequiredKeyedService<ServiceBusProcessor>(key)));
            }
        }
    }
}