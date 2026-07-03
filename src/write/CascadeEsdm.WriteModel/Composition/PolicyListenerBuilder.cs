using System.Text.Json;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.WriteModel.Policies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.WriteModel.Composition;

public class PolicyListenerBuilder
{
    private readonly IServiceCollection _services;
    private readonly string? _name;
    private JsonSerializerOptions? _serializerOptions;
    private Type? _exceptionHandlerType;

    internal PolicyListenerBuilder(IServiceCollection services, string? name = null)
    {
        _services = services;
        _name = name;
    }

    public PolicyListenerBuilder WithSerialisationSettings(JsonSerializerOptions options)
    {
        _serializerOptions = options;
        return this;
    }

    public PolicyListenerBuilder WithExceptionHandler<THandler>()
        where THandler : class, IMessageExceptionHandler
    {
        _exceptionHandlerType = typeof(THandler);
        return this;
    }

    internal void Build()
    {
        if (!_services.Any(s => s.ServiceType == typeof(IPolicyDispatcher)))
            throw new InvalidOperationException(
                "IPolicyDispatcher is not registered. Call UsingPolicies before UsingPolicyListener.");

        var receiverRegistered = _name == null
            ? _services.Any(s => s.ServiceType == typeof(IMessageReceiver) && !s.IsKeyedService)
            : _services.Any(s => s.ServiceType == typeof(IMessageReceiver) && s.IsKeyedService && Equals(s.ServiceKey, _name));

        if (!receiverRegistered)
            throw new InvalidOperationException(
                _name == null
                    ? "IMessageReceiver is not registered. Call UsingAzureServiceBusPolicyListener before UsingPolicyListener."
                    : $"No IMessageReceiver registered with key '{_name}'. Call UsingAzureServiceBusPolicyListener with the matching name.");

        var options = _serializerOptions ?? DefaultSerialisationSettings.ForMessageBus();

        _services.AddTransient<IHostedService>(sp =>
        {
            var receiver = sp.GetRequiredKeyedService<IMessageReceiver>(_name);
            var dispatcher = sp.GetRequiredService<IPolicyDispatcher>();
            var logger = sp.GetRequiredService<ILogger<PolicyListener>>();
            var exceptionHandler = _exceptionHandlerType != null
                ? (IMessageExceptionHandler)sp.GetRequiredService(_exceptionHandlerType)
                : new DefaultMessageExceptionHandler();
            return new PolicyListener(dispatcher, receiver, exceptionHandler, logger, options);
        });
    }
}
