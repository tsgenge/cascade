# Policy Listener

## Overview

The policy listener bridges an external message bus to the [policy dispatcher](Policies.md). It runs as an `IHostedService`, receiving serialised `EventEnvelope` messages from an `IMessageReceiver`, deserialising them, and dispatching them to registered policies via `IPolicyDispatcher`.

This decouples event consumption from event production — the write model publishes events to a message bus topic; a separate host subscribes, deserialises, and dispatches them through the same policy infrastructure.

## Concepts

| Term | Description |
|---|---|
| **Message** | A transport-agnostic envelope carrying a `Body` (string) and `ApplicationProperties` (`IReadOnlyDictionary<string, object>`) |
| **MessageAction** | The action to apply to a message after processing: `Complete`, `Abandon`, `DeadLetter`, `Schedule` |
| **IMessageReceiver** | Transport-agnostic interface for receiving messages — `StartAsync`, `StopAsync`, `ApplyActionAsync` |
| **IMessageExceptionHandler** | Decides what `MessageAction` to apply when message processing fails |
| **PolicyListener** | The `IHostedService` that wires `IMessageReceiver` to `IPolicyDispatcher` |
| **DefaultMessageExceptionHandler** | Built-in handler that always returns `MessageAction.DeadLetter` |

## How It Works

```
Message Bus (e.g. Azure Service Bus topic/subscription)
    │
    ▼
IMessageReceiver.StartAsync(handler)
    │
    ▼
PolicyListener.HandleMessageAsync(message)
    │
    ├── Deserialise message.Body → EventEnvelope
    ├── IPolicyDispatcher.DispatchAsync(envelope)
    ├── On success → ApplyActionAsync(Complete)
    └── On exception → IMessageExceptionHandler.HandleAsync()
                          → ApplyActionAsync(returned action)
```

## Composition

Wire up the policy listener using `UsingPolicyListener` on the `WriteModelBuilder`, after `UsingPolicies`:

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UsingCosmosDbStorage(storage => storage
            .WithConnectionString(cosmosConnection)
            .WithDatabaseName("cascade")
            .WithEventStreamContainer<EventStreamContainer>())
        .UsingAzureDistributedLocks(locks => locks
            .WithConnectionString(storageConnection))
        .UsingApplicationInsights()
        .UsingAzureServiceBusPolicyListener(asb => asb
            .WithConnectionString(serviceBusConnection)
            .WithTopic("domain-events")
            .WithSubscription("policy-handler")))
    .WithWriteModel(write => write
        .UsingExecutors(executors => executors
            .AddCommandsFromAssembly<OrderAggregate>())
        .UsingAppliers(appliers => appliers
            .AddEventAppliersFromAssembly<OrderAggregate>())
        .UsingPolicies(policies => policies
            .AddPoliciesFromAssembly<OrderAggregate>())
        .UsingPolicyListener()));
```

### Validation

`UsingPolicyListener` validates at startup that:
- `IPolicyDispatcher` is registered (call `UsingPolicies` first)
- `IMessageReceiver` is registered (call `UsingAzureServiceBusPolicyListener` or register a custom implementation first)

If either is missing, an `InvalidOperationException` is thrown with a clear message.

### Default Exception Handler

If no `IMessageExceptionHandler` is registered, `UsingPolicyListener` registers `DefaultMessageExceptionHandler`, which always returns `MessageAction.DeadLetter`. To override, implement `IMessageExceptionHandler` and register it before calling `UsingPolicyListener`:

```csharp
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;

internal class RetryableExceptionHandler : IMessageExceptionHandler
{
    private readonly ILogger<RetryableExceptionHandler> _logger;

    public RetryableExceptionHandler(ILogger<RetryableExceptionHandler> logger)
    {
        _logger = logger;
    }

    public Task<MessageAction> HandleAsync(Message message, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Failed to process message");

        if (exception is TransientException)
            return Task.FromResult(MessageAction.Abandon);

        return Task.FromResult(MessageAction.DeadLetter);
    }
}
```

Register the custom handler in the DI container:

```csharp
services.AddSingleton<IMessageExceptionHandler, RetryableExceptionHandler>();
```

### Custom Serialisation

By default, `PolicyListener` uses `DefaultSerialisationSettings.ForMessageBus()` for deserialisation. Override via the builder:

```csharp
.UsingPolicyListener(listener => listener
    .WithSerialisationSettings(myCustomOptions))
```

## Abstractions

### Message

```csharp
namespace CascadeEsdm.SharedKernel.Infrastructure.Messaging;

public record Message
{
    public string Body { get; }
    public IReadOnlyDictionary<string, object> ApplicationProperties { get; }
}
```

`ApplicationProperties` uses `object` values to preserve the native types of the underlying transport (e.g. Azure Service Bus supports `string`, `int`, `bool`, `Guid`, `DateTime`, etc.).

### MessageAction

```csharp
public enum MessageAction
{
    Complete,   // message processed successfully
    Abandon,    // release the message back to the queue for retry
    DeadLetter, // move to the dead-letter queue
    Schedule    // schedule for later redelivery
}
```

### IMessageReceiver

```csharp
public interface IMessageReceiver
{
    Task StartAsync(Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task ApplyActionAsync(Message message, MessageAction action, CancellationToken cancellationToken);
}
```

Transport implementations (e.g. `AzureServiceBusReceiver`) implement this interface. The `PolicyListener` calls `StartAsync` with its internal handler and delegates message settlement to `ApplyActionAsync`.

### IMessageExceptionHandler

```csharp
public interface IMessageExceptionHandler
{
    Task<MessageAction> HandleAsync(Message message, Exception exception, CancellationToken cancellationToken);
}
```

Implement this to control what happens when message processing fails — log, inspect the exception, and return the appropriate `MessageAction`.

## Azure Service Bus Infrastructure

Install the `CascadeEsdm.Messaging.AzureServiceBus` package to use Azure Service Bus as the message transport:

```bash
dotnet add package CascadeEsdm.Messaging.AzureServiceBus
```

### Configuration

```csharp
infra.UsingAzureServiceBusPolicyListener(asb => asb
    .WithConnectionString(connectionString)  // required
    .WithTopic(topicName)                    // required
    .WithSubscription(subscriptionName))     // required
```

All three settings are required — an `InvalidOperationException` is thrown if any is missing.

### How It Works

`AzureServiceBusReceiver` wraps a `ServiceBusProcessor` from the `Azure.Messaging.ServiceBus` SDK:

- **StartAsync** — subscribes to `ProcessMessageAsync` and `ProcessErrorAsync` on the processor, then calls `StartProcessingAsync`
- **StopAsync** — calls `StopProcessingAsync`
- **OnProcessMessageAsync** — maps `ServiceBusReceivedMessage` to a `Message` (body as UTF-8 string, all `ApplicationProperties` preserved), then invokes the handler
- **ApplyActionAsync** — downcasts the `Message` to `AzureServiceBusMessage` (a record subclass carrying the original `ServiceBusReceivedMessage` and `ProcessMessageEventArgs`) and calls the appropriate settlement method (`CompleteMessageAsync`, `AbandonMessageAsync`, or `DeadLetterMessageAsync`)

The `ServiceBusProcessor` and `AzureServiceBusReceiver` are registered as singletons because `ServiceBusProcessor` is a long-lived object designed for the lifetime of the application.

## Implementing a Custom Transport

To use a different message bus, implement `IMessageReceiver`:

```csharp
internal class RabbitMqReceiver : IMessageReceiver
{
    public Task StartAsync(Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        // Subscribe to the queue/exchange and invoke handler for each message
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Unsubscribe / close connection
    }

    public Task ApplyActionAsync(Message message, MessageAction action, CancellationToken cancellationToken)
    {
        // Ack, nack, or dead-letter based on the action
    }
}
```

Register it in the DI container before calling `UsingPolicyListener`:

```csharp
services.AddSingleton<IMessageReceiver, RabbitMqReceiver>();
```
