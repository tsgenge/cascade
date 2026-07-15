using Azure.Messaging.ServiceBus;
using CascadeEsdm.Messaging.AzureServiceBus;
using CascadeEsdm.OtherTestDomain.Schema.Domain.Monsters.Events;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

internal static class PolicyPartitioningTestHelpers
{
    public static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan NoReceiveTimeout = TimeSpan.FromSeconds(3);

    public static async Task SendEventAsync(IServiceProvider serviceProvider, string clientKey, string topic)
    {
        var envelope = new EventEnvelope(
            new EventSource("MonsterAggregate", Guid.NewGuid(), "EatPerson"),
            new Subject(Guid.NewGuid(), "Monster"),
            AuthenticatedContext.Empty,
            ClientChannel.Empty,
            new PersonEaten(Guid.NewGuid(), 500), 0);

        var payload = JsonSerializer.Serialize(envelope, DefaultSerialisationSettings.ForMessageBus());

        var client = serviceProvider.GetRequiredKeyedService<ServiceBusClient>(clientKey);
        var sender = client.CreateSender(topic);
        await sender.SendMessageAsync(new ServiceBusMessage(payload) { SessionId = envelope.Subject.Value });
    }

    public static Task SendToUnkeyedStreamAsync(IServiceProvider serviceProvider, string topic = "example-stream")
        => SendEventAsync(serviceProvider, nameof(ServiceBusReceiverBuilder), topic);

    public static async Task<bool> ReceivedAsync<TCommand>(IServiceProvider serviceProvider, TimeSpan timeout)
        where TCommand : ICommand
    {
        var channel = serviceProvider.GetRequiredService<MessageChannel<TCommand>>();
        try {
            await channel.WaitForNextAsync(timeout);
            return true;
        }
        catch (OperationCanceledException) {
            return false;
        }
    }

    public static async Task ClearAsync<TCommand>(IServiceProvider serviceProvider)
        where TCommand : ICommand
    {
        var channel = serviceProvider.GetRequiredService<MessageChannel<TCommand>>();
        await channel.Clear();
    }
}
