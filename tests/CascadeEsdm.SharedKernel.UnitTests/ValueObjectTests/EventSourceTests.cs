using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.TestDomain.People;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.ValueObjectTests;

public class EventSourceTests
{
    [Fact]
    public void Constructor_FromComponents_CreatesEventSource()
    {
        var aggregate = "TestAssembly/TestAggregate";
        var commandId = Guid.NewGuid();
        var command = "TestCommand";

        var eventSource = new EventSource(aggregate, commandId, command);

        eventSource.Aggregate.Should().Be(aggregate);
        eventSource.CommandId.Should().Be(commandId);
        eventSource.Command.Should().Be(command);
        eventSource.Value.Should().Be($"{aggregate}/{command}/{commandId}");
    }

    [Fact]
    public void Constructor_FromComponents_AggregateWithoutSlash_AppendsName()
    {
        var aggregate = "TestAggregate";
        var commandId = Guid.NewGuid();
        var command = "TestCommand";

        var eventSource = new EventSource(aggregate, commandId, command);

        eventSource.Aggregate.Should().Be("TestAggregate/TestAggregate");
    }

    [Fact]
    public void Constructor_FromString_ParsesEventSource()
    {
        var commandId = Guid.NewGuid();
        var value = $"TestAssembly/TestAggregate/TestCommand/{commandId}";

        var eventSource = new EventSource(value);

        eventSource.Aggregate.Should().Be("TestAssembly/TestAggregate");
        eventSource.Command.Should().Be("TestCommand");
        eventSource.CommandId.Should().Be(commandId);
    }

    [Fact]
    public void Constructor_FromString_ParsesEventSourceWithHyphenatedGuid()
    {
        var commandId = Guid.NewGuid();
        var value = $"TestAssembly/TestAggregate/TestCommand/{commandId:D}";

        var eventSource = new EventSource(value);

        eventSource.Aggregate.Should().Be("TestAssembly/TestAggregate");
        eventSource.Command.Should().Be("TestCommand");
        eventSource.CommandId.Should().Be(commandId);
    }

    [Fact]
    public void Constructor_FromString_ParsesEventSourceWithBracedGuid()
    {
        var commandId = Guid.NewGuid();
        var value = $"TestAssembly/TestAggregate/TestCommand/{{{commandId:D}}}";

        var eventSource = new EventSource(value);

        eventSource.Aggregate.Should().Be("TestAssembly/TestAggregate");
        eventSource.Command.Should().Be("TestCommand");
        eventSource.CommandId.Should().Be(commandId);
    }

    [Fact]
    public void Constructor_FromString_InvalidFormat_ThrowsArgumentException()
    {
        var invalidValue = "invalid-format";

        Action act = () => new EventSource(invalidValue);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ForAggregate_WithType_CreatesEventSource()
    {
        var commandId = Guid.NewGuid();
        var command = "TestCommand";

        var eventSource = EventSource.ForAggregate(typeof(PersonAggregate), commandId, command);

        eventSource.Aggregate.Should().EndWith("/PersonAggregate");
        eventSource.CommandId.Should().Be(commandId);
        eventSource.Command.Should().Be(command);
    }

    [Fact]
    public void ForAggregate_Generic_WithCommandType_CreatesEventSource()
    {
        var commandId = Guid.NewGuid();

        var eventSource = EventSource.ForAggregate<PersonAggregate, TestCommand>(commandId);

        eventSource.Aggregate.Should().EndWith("/PersonAggregate");
        eventSource.CommandId.Should().Be(commandId);
        eventSource.Command.Should().Be("TestCommand");
    }

    [Fact]
    public void ForAggregate_Generic_WithCommandTypeName_CreatesEventSource()
    {
        var commandId = Guid.NewGuid();

        var eventSource = EventSource.ForAggregate<PersonAggregate>(commandId, "TestCommand");

        eventSource.Aggregate.Should().EndWith("/PersonAggregate");
        eventSource.CommandId.Should().Be(commandId);
        eventSource.Command.Should().Be("TestCommand");
    }

    private class TestCommand { }
}
