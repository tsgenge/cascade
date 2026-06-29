using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;
using System.Text.Json;

namespace CascadeEsdm.SharedKernel.UnitTests.ValueObjectTests;

public class ValueObjectConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ValueObjectConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new ValueObjectConverter());
    }

    [Fact]
    public void Serialize_Tenant_WritesGuidValue()
    {
        var id = Guid.NewGuid();
        var tenant = new Tenant(id);

        var json = JsonSerializer.Serialize(tenant, _options);

        json.Should().Be($"\"{id}\"");
    }

    [Fact]
    public void Deserialize_Tenant_ReadsTenantFromGuid()
    {
        var id = Guid.NewGuid();
        var json = $"\"{id}\"";

        var tenant = JsonSerializer.Deserialize<Tenant>(json, _options);

        tenant.Should().NotBeNull();
        tenant!.Value.Should().Be(id);
    }

    [Fact]
    public void RoundTrip_Tenant_PreservesValue()
    {
        var original = new Tenant(Guid.NewGuid());

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Tenant>(json, _options);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void Serialize_UserIdentity_WritesGuidValue()
    {
        var id = Guid.NewGuid();
        var identity = new UserIdentity(id);

        var json = JsonSerializer.Serialize(identity, _options);

        json.Should().Be($"\"{id}\"");
    }

    [Fact]
    public void Deserialize_UserIdentity_ReadsFromGuidString()
    {
        var id = Guid.NewGuid();
        var json = $"\"{id}\"";

        var identity = JsonSerializer.Deserialize<UserIdentity>(json, _options);

        identity.Should().NotBeNull();
        identity!.Value.Should().Be(id);
    }

    [Fact]
    public void RoundTrip_UserIdentity_PreservesValue()
    {
        var original = new UserIdentity(Guid.NewGuid());

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<UserIdentity>(json, _options);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void Serialize_ClientChannel_WritesStringValue()
    {
        var channel = new ClientChannel("validChannel12345");

        var json = JsonSerializer.Serialize(channel, _options);

        json.Should().Be("\"validChannel12345\"");
    }

    [Fact]
    public void Deserialize_ClientChannel_ReadsFromString()
    {
        var json = "\"validChannel12345\"";

        var channel = JsonSerializer.Deserialize<ClientChannel>(json, _options);

        channel.Should().NotBeNull();
        channel!.Value.Should().Be("validChannel12345");
    }

    [Fact]
    public void RoundTrip_ClientChannel_PreservesValue()
    {
        var original = new ClientChannel("validChannel12345");

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<ClientChannel>(json, _options);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void Serialize_Subject_WritesStringValue()
    {
        var id = Guid.NewGuid();
        var subject = new Subject(id, "TestAggregate");

        var json = JsonSerializer.Serialize(subject, _options);

        json.Should().Be($"\"TestAggregate/{id:n}\"");
    }

    [Fact]
    public void Deserialize_Subject_ReadsFromFormattedString()
    {
        var id = Guid.NewGuid();
        var json = $"\"TestAggregate/{id:n}\"";

        var subject = JsonSerializer.Deserialize<Subject>(json, _options);

        subject.Should().NotBeNull();
        subject!.Id.Should().Be(id);
        subject.Type.Should().Be("TestAggregate");
    }

    [Fact]
    public void RoundTrip_Subject_PreservesValue()
    {
        var original = new Subject(Guid.NewGuid(), "TestAggregate");

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Subject>(json, _options);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void Serialize_EventSource_WritesStringValue()
    {
        var commandId = Guid.NewGuid();
        var eventSource = new EventSource("Assembly/Aggregate", commandId, "Command");

        var json = JsonSerializer.Serialize(eventSource, _options);

        json.Should().Be($"\"Assembly/Aggregate/Command/{commandId}\"");
    }

    [Fact]
    public void Deserialize_EventSource_ReadsFromFormattedString()
    {
        var commandId = Guid.NewGuid();
        var json = $"\"Assembly/Aggregate/Command/{commandId}\"";

        var eventSource = JsonSerializer.Deserialize<EventSource>(json, _options);

        eventSource.Should().NotBeNull();
        eventSource!.Aggregate.Should().Be("Assembly/Aggregate");
        eventSource.Command.Should().Be("Command");
        eventSource.CommandId.Should().Be(commandId);
    }

    [Fact]
    public void RoundTrip_EventSource_PreservesValue()
    {
        var original = new EventSource("Assembly/Aggregate", Guid.NewGuid(), "Command");

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<EventSource>(json, _options);

        deserialized.Should().Be(original);
    }

    [Fact]
    public void CanConvert_ValueObjectType_ReturnsTrue()
    {
        var converter = new ValueObjectConverter();

        converter.CanConvert(typeof(Tenant)).Should().BeTrue();
        converter.CanConvert(typeof(UserIdentity)).Should().BeTrue();
        converter.CanConvert(typeof(ClientChannel)).Should().BeTrue();
        converter.CanConvert(typeof(Subject)).Should().BeTrue();
        converter.CanConvert(typeof(EventSource)).Should().BeTrue();
    }

    [Fact]
    public void CanConvert_NonValueObjectType_ReturnsFalse()
    {
        var converter = new ValueObjectConverter();

        converter.CanConvert(typeof(string)).Should().BeFalse();
        converter.CanConvert(typeof(int)).Should().BeFalse();
        converter.CanConvert(typeof(Guid)).Should().BeFalse();
    }
}
