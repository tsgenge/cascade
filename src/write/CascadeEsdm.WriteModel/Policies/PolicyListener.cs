using System.Text.Json;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.WriteModel.Policies;

internal class PolicyListener : IHostedService
{
    private readonly IPolicyDispatcher _policyDispatcher;
    private readonly IMessageReceiver _messageReceiver;
    private readonly IMessageExceptionHandler _exceptionHandler;
    private readonly ILogger<PolicyListener> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public PolicyListener(
        IPolicyDispatcher policyDispatcher,
        IMessageReceiver messageReceiver,
        IMessageExceptionHandler exceptionHandler,
        ILogger<PolicyListener> logger,
        JsonSerializerOptions serializerOptions)
    {
        _policyDispatcher = policyDispatcher;
        _messageReceiver = messageReceiver;
        _exceptionHandler = exceptionHandler;
        _logger = logger;
        _serializerOptions = serializerOptions;
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
            var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Body, _serializerOptions)
                ?? throw new JsonException("Deserialised EventEnvelope was null.");

            await _policyDispatcher.DispatchAsync(envelope, cancellationToken);
            await _messageReceiver.ApplyActionAsync(message, MessageAction.Complete, cancellationToken);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error processing message");
            var action = await _exceptionHandler.HandleAsync(message, ex, cancellationToken);
            await _messageReceiver.ApplyActionAsync(message, action, cancellationToken);
        }
    }
}
