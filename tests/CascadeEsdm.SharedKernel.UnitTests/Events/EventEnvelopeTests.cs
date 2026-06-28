using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Events;

public class EventEnvelopeTests
{
    private record TestEvent : IDomainEvent;

    [Fact]
    public void Constructor_FromComponents_SetsProperties()
    {
        var source = new EventSource("TestAssembly/TestAggregate", Guid.NewGuid(), "TestCommand");
        var subject = new Subject(Guid.NewGuid(), "TestAggregate");
        var securityContext = AuthenticatedContext.Empty;
        var channel = ClientChannel.Empty;
        var domainEvent = new TestEvent();
        var sequence = 5;

        var envelope = new EventEnvelope(source, subject, securityContext, channel, domainEvent, sequence);

        envelope.Source.Should().Be(source);
        envelope.Subject.Should().Be(subject);
        envelope.SecurityContext.Should().Be(securityContext);
        envelope.Channel.Should().Be(channel);
        envelope.Event.Should().Be(domainEvent);
        envelope.Sequence.Should().Be(sequence);
        envelope.Type.Should().Be(nameof(TestEvent));
        envelope.Id.Should().NotBe(Guid.Empty);
        envelope.Time.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void JsonConstructor_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var source = new EventSource("TestAssembly/TestAggregate", Guid.NewGuid(), "TestCommand");
        var subject = new Subject(Guid.NewGuid(), "TestAggregate");
        var securityContext = AuthenticatedContext.Empty;
        var channel = ClientChannel.Empty;
        var domainEvent = new TestEvent();
        var sequence = 3;
        var type = "TestEvent";
        var time = DateTimeOffset.UtcNow.AddHours(-1);

        var envelope = new EventEnvelope(id, source, subject, securityContext, channel, domainEvent, sequence, type, time);

        envelope.Id.Should().Be(id);
        envelope.Source.Should().Be(source);
        envelope.Subject.Should().Be(subject);
        envelope.SecurityContext.Should().Be(securityContext);
        envelope.Channel.Should().Be(channel);
        envelope.Event.Should().Be(domainEvent);
        envelope.Sequence.Should().Be(sequence);
        envelope.Type.Should().Be(type);
        envelope.Time.Should().Be(time);
    }

    [Fact]
    public void Constructor_FromComponents_GeneratesUniqueIds()
    {
        var source = new EventSource("TestAssembly/TestAggregate", Guid.NewGuid(), "TestCommand");
        var subject = new Subject(Guid.NewGuid(), "TestAggregate");
        var securityContext = AuthenticatedContext.Empty;
        var channel = ClientChannel.Empty;
        var domainEvent = new TestEvent();

        var envelope1 = new EventEnvelope(source, subject, securityContext, channel, domainEvent, 1);
        var envelope2 = new EventEnvelope(source, subject, securityContext, channel, domainEvent, 2);

        envelope1.Id.Should().NotBe(envelope2.Id);
    }
}
