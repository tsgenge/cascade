using CascadeEsdm.OtherTestDomain.Domain.Monsters;
using CascadeEsdm.OtherTestDomain.Domain.Monsters.Commands;
using CascadeEsdm.OtherTestDomain.Domain.Monsters.Events;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CascadeEsdm.SharedKernel.UnitTests.Serialisation;

public class SerialisationTypeEncodingTests
{
    [Fact]
    public void Serialised_And_Schema_Namespaces_Match()
    {
        var envelope = new EventEnvelope(
            EventSource.ForAggregate<MonsterAggregate>(Guid.NewGuid(), nameof(EatPerson)),
            new Subject(Guid.NewGuid(), "Monster"),
            AuthenticatedContext.Empty,
            ClientChannel.Empty,
            new OtherTestDomain.Domain.Monsters.Events.PersonEaten(Guid.NewGuid(), 10),
            0
            );

        var payload = JsonSerializer.Serialize(envelope, DefaultSerialisationSettings.ForMessageBus());
        var json = JsonNode.Parse(payload);
        var expectedType = typeof(OtherTestDomain.Schema.Domain.Monsters.Events.PersonEaten);
        var eventType = json?["event"]?["$type"]?.GetValue<string>();
        var expectedName = expectedType.AssemblyQualifiedName.Substring(0, expectedType.AssemblyQualifiedName.IndexOf("Version=") - 2);
        eventType.Should().Be(expectedName);
    }
}