using System.Text.Json;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.WriteModel.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Composition;

public class PolicyListenerBuilder
{
    private readonly IServiceCollection _services;
    private JsonSerializerOptions? _serializerOptions;

    internal PolicyListenerBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public PolicyListenerBuilder WithSerialisationSettings(JsonSerializerOptions options)
    {
        _serializerOptions = options;
        return this;
    }

    internal void Build()
    {
        if (!_services.Any(s => s.ServiceType == typeof(IPolicyDispatcher)))
            throw new InvalidOperationException(
                "IPolicyDispatcher is not registered. Call UsingPolicies before UsingPolicyListener.");

        if (!_services.Any(s => s.ServiceType == typeof(IMessageReceiver)))
            throw new InvalidOperationException(
                "IMessageReceiver is not registered. Register an IMessageReceiver implementation before calling UsingPolicyListener.");

        var options = _serializerOptions ?? DefaultSerialisationSettings.ForMessageBus();
        _services.AddSingleton(options);

        if (!_services.Any(s => s.ServiceType == typeof(IMessageExceptionHandler)))
            _services.AddSingleton<IMessageExceptionHandler, DefaultMessageExceptionHandler>();

        _services.AddHostedService<PolicyListener>();
    }
}
