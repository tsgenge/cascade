using Azure.Messaging.ServiceBus;
using CascadeEsdm.Messaging.AzureServiceBus;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.TestDomain.Monsters;
using CascadeEsdm.TestDomain.Monsters.Commands;
using CascadeEsdm.TestDomain.People.Commands;
using CascadeEsdm.TestDomain.People.Events;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.TestDomain.Schema.Monsters.Events;
using CascadeEsdm.Testing;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class PolicyListeningTests : TestBase
{
    public PolicyListeningTests(ITestOutputHelper output, WriteContext environment) : base(output, environment) { }

    [Fact]
    public async Task Policy_Listens_And_Invokes_Command()
    {
        var channel = Environment.ServiceProvider.GetRequiredService<MessageChannel<RemovePerson>>();

        var addHandler = Environment.ServiceProvider.GetRequiredService<ICommandHandler<AddPerson>>();
        var result = await addHandler.HandleAsync(new CommandEnvelope<AddPerson>(
            new AddPerson(new FirstName("Tim"), new LastName("Here"), new MobileNumber("07000000000")),
            AuthenticatedContext.Empty,
            ClientChannel.Empty));
        var addEvent = result.Events.First(e => e.Type == nameof(PersonAdded));

        var envelope = new EventEnvelope(
            EventSource.ForAggregate(typeof(MonsterAggregate), Guid.NewGuid(), nameof(EatPerson)),
            new Subject(Guid.NewGuid(), "Monster"),
            AuthenticatedContext.Empty,
            ClientChannel.Empty,
            new PersonEaten(addEvent.Subject.Id, 500), 0);

        var payload = JsonSerializer.Serialize(envelope, DefaultSerialisationSettings.ForMessageBus());

        var client =
            Environment.ServiceProvider.GetRequiredKeyedService<ServiceBusClient>(nameof(ServiceBusReceiverBuilder));
        var sender = client.CreateSender("example-stream");
        await sender.SendMessageAsync(new ServiceBusMessage(payload) { SessionId = envelope.Subject.Value });

        var policyResult = await channel.WaitForNextAsync(TimeSpan.FromSeconds(5));
        policyResult.Should().NotBeNull();
        policyResult.Should().Be(new RemovePerson(new PersonId(addEvent.Subject.Id)));
    }
}