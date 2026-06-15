using Azure.Core.Serialization;
using CascadeEsdm.ReadModel.Infrastructure;
using CascadeEsdm.SharedKernel.Composition;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeEsdm.SignalR;

public class SignalRBuilder
{
    private readonly InfrastructureBuilder _infraBuilder;
    private string? _connectionString;
    private string? _hubName;

    public SignalRBuilder(InfrastructureBuilder infrastructure)
    {
        _infraBuilder = infrastructure ?? throw new ArgumentNullException(nameof(infrastructure));
    }

    public SignalRBuilder WithConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }

    public SignalRBuilder WithHubName(string hubName)
    {
        _hubName = hubName;
        return this;
    }

    public void Build()
    {
        // Validate settings
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("SignalR connection string is required, use WithConnectionString().");

        if (string.IsNullOrEmpty(_hubName))
            throw new InvalidOperationException("SignalR hub name is required, use WithHubName()");

        _infraBuilder.Services.AddSingleton(sp =>
        {
            return new ServiceManagerBuilder()
                .WithOptions(o =>
                {
                    o.ConnectionString = _connectionString;
                    o.ServiceTransportType = ServiceTransportType.Transient;
                    o.UseJsonObjectSerializer(new JsonObjectSerializer(new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                    }));
                })
                .BuildServiceManager();
        });

        _infraBuilder.Services.AddSingleton(sp =>
            {
                var sm = sp.GetRequiredService<ServiceManager>();
                return sm.CreateHubContextAsync(_hubName, CancellationToken.None).ConfigureAwait(false).GetAwaiter()
                    .GetResult();
            }
        );

        _infraBuilder.Services.AddScoped<IViewNotificationService, SignalRViewNotifier>();
    }
}