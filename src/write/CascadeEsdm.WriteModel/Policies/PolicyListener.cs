using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CascadeEsdm.WriteModel.Policies;

internal class PolicyListener : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageReceiver _messageReceiver;    
    private readonly IMessageExceptionHandler _exceptionHandler;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<PolicyListener> _logger;

    public PolicyListener(IServiceScopeFactory scopeFactory, IMessageReceiver messageReceiver,
        IMessageExceptionHandler exceptionHandler, JsonSerializerOptions serializerOptions,
        ILogger<PolicyListener> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _messageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _messageReceiver.StartAsync(HandleMessageAsync, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _messageReceiver.StopAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try {
            using var scope = _scopeFactory.CreateScope();
            
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Body, _serializerOptions)
                           ?? throw new JsonException("Deserialised EventEnvelope was null.");

            var telemetryLogger = scope.ServiceProvider.GetRequiredService<ITelemetryLogger>();
            using var op = telemetryLogger.StartOperation($"Processing [{envelope?.Type}] message", null, TelemetryOperationKind.Consumer);
            
            var dispatcher = scope.ServiceProvider.GetRequiredService<IPolicyDispatcher>();
            await dispatcher.DispatchAsync(envelope!, cancellationToken);
            await _messageReceiver.ApplyActionAsync(message, MessageAction.Complete, null, cancellationToken);
        }
        catch (Exception ex) {
            var msg = "Error processing or deserialising message";
            _logger.LogError(ex, msg);
            var inner = new Exception(msg, ex);
            var action = await _exceptionHandler.HandleAsync(message, inner, cancellationToken);
            await _messageReceiver.ApplyActionAsync(message, action, inner, cancellationToken);
        }
    }
}