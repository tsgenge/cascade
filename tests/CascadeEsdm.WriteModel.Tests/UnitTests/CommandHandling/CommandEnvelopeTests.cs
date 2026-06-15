using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class CommandEnvelopeTests
{
    [Fact]
    public void Constructor_WithCommand_SecurityContext_AndChannel_SetsProperties()
    {
        var command = new TestCommand(Guid.NewGuid());
        var securityContext = new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid()));
        var channel = new ClientChannel("test-channel");

        var envelope = new CommandEnvelope<TestCommand>(command, securityContext, channel);

        envelope.Command.Should().Be(command);
        envelope.SecurityContext.Should().Be(securityContext);
        envelope.Channel.Should().Be(channel);
    }

    [Fact]
    public void Constructor_SetsTypeToCommandTypeName()
    {
        var command = new TestCommand(Guid.NewGuid());
        var securityContext = new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid()));
        var channel = new ClientChannel("test-channel");

        var envelope = new CommandEnvelope<TestCommand>(command, securityContext, channel);

        envelope.Type.Should().Be(nameof(TestCommand));
    }

    [Fact]
    public void Constructor_GeneratesUniqueId()
    {
        var command = new TestCommand(Guid.NewGuid());
        var securityContext = new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid()));
        var channel = new ClientChannel("test-channel");

        var envelope1 = new CommandEnvelope<TestCommand>(command, securityContext, channel);
        var envelope2 = new CommandEnvelope<TestCommand>(command, securityContext, channel);

        envelope1.Id.Should().NotBeEmpty();
        envelope2.Id.Should().NotBeEmpty();
        envelope1.Id.Should().NotBe(envelope2.Id);
    }

    [Fact]
    public void Constructor_SetsTimeToCurrentUtc()
    {
        var before = DateTimeOffset.UtcNow.AddMilliseconds(-100);
        var command = new TestCommand(Guid.NewGuid());
        var securityContext = new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid()));
        var channel = new ClientChannel("test-channel");

        var envelope = new CommandEnvelope<TestCommand>(command, securityContext, channel);

        var after = DateTimeOffset.UtcNow.AddMilliseconds(100);
        envelope.Time.Should().BeOnOrAfter(before);
        envelope.Time.Should().BeOnOrBefore(after);
        envelope.Time.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void JsonConstructor_WithAllProperties_SetsValuesCorrectly()
    {
        var id = Guid.NewGuid();
        var type = "TestCommand";
        var command = new TestCommand(Guid.NewGuid());
        var securityContext = new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid()));
        var channel = new ClientChannel("test-channel");
        var time = DateTimeOffset.UtcNow.AddHours(-1);

        var envelope = new CommandEnvelope<TestCommand>(id, type, command, securityContext, channel, time);

        envelope.Id.Should().Be(id);
        envelope.Type.Should().Be(type);
        envelope.Command.Should().Be(command);
        envelope.SecurityContext.Should().Be(securityContext);
        envelope.Channel.Should().Be(channel);
        envelope.Time.Should().Be(time);
    }

    [Fact]
    public void Implements_ICommandEnvelope()
    {
        var command = new TestCommand(Guid.NewGuid());
        var securityContext = new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid()));
        var channel = new ClientChannel("test-channel");

        ICommandEnvelope envelope = new CommandEnvelope<TestCommand>(command, securityContext, channel);

        envelope.Should().NotBeNull();
        envelope.Command.Should().Be(command);
    }

    [Fact]
    public void Implements_ICommandEnvelopeOfT()
    {
        var command = new TestCommand(Guid.NewGuid());
        var securityContext = new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid()));
        var channel = new ClientChannel("test-channel");

        ICommandEnvelope<TestCommand> envelope = new CommandEnvelope<TestCommand>(command, securityContext, channel);

        envelope.Should().NotBeNull();
        envelope.Command.Should().Be(command);
        envelope.Command.Id.Should().Be(command.Id);
    }
}
