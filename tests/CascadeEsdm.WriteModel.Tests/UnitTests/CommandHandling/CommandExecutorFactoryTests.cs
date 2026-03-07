using AutoFixture;
using AutoFixture.Xunit2;
using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class CommandExecutorFactoryTests
{
    private readonly IFixture _fixture;

    public CommandExecutorFactoryTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Constructor_WithNullExecutors_ThrowsArgumentNullException()
    {
        var act = () => new CommandExecutorFactory<TestAggregate>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("executors");
    }

    [Fact]
    public void Constructor_WithValidExecutors_CreatesInstance()
    {
        var executors = Array.Empty<ICommandExecutor<TestAggregate>>();

        var act = () => new CommandExecutorFactory<TestAggregate>(executors);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetFor_WithMatchingExecutor_ReturnsExecutor()
    {
        var expectedExecutor = Substitute.For<ICommandExecutor<TestCommand, TestAggregate>>();
        var executors = new ICommandExecutor<TestAggregate>[] { expectedExecutor };
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var result = factory.GetFor<TestCommand>();

        result.Should().BeSameAs(expectedExecutor);
    }

    [Fact]
    public void GetFor_WithNoMatchingExecutor_ThrowsUnknownCommandException()
    {
        var executors = Array.Empty<ICommandExecutor<TestAggregate>>();
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var act = () => factory.GetFor<TestCommand>();

        act.Should().Throw<UnknownCommandException>()
            .Which.Command.Should().Be(nameof(TestCommand));
    }

    [Fact]
    public void GetFor_WithNoMatchingExecutor_ThrowsExceptionWithCorrectAggregateSource()
    {
        var executors = Array.Empty<ICommandExecutor<TestAggregate>>();
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var act = () => factory.GetFor<TestCommand>();

        act.Should().Throw<UnknownCommandException>()
            .Which.AggregateSource.Should().Be(nameof(TestAggregate));
    }

    [Fact]
    public void GetFor_WithMultipleExecutorsIncludingMatch_ReturnsCorrectExecutor()
    {
        var otherExecutor = Substitute.For<ICommandExecutor<OtherTestCommand, TestAggregate>>();
        var expectedExecutor = Substitute.For<ICommandExecutor<TestCommand, TestAggregate>>();
        var executors = new ICommandExecutor<TestAggregate>[] { otherExecutor, expectedExecutor };
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var result = factory.GetFor<TestCommand>();

        result.Should().BeSameAs(expectedExecutor);
    }

    [Fact]
    public void GetFor_WithMultipleExecutorsNoMatch_ThrowsUnknownCommandException()
    {
        var executor1 = Substitute.For<ICommandExecutor<OtherTestCommand, TestAggregate>>();
        var executor2 = Substitute.For<ICommandExecutor<AnotherTestCommand, TestAggregate>>();
        var executors = new ICommandExecutor<TestAggregate>[] { executor1, executor2 };
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var act = () => factory.GetFor<TestCommand>();

        act.Should().Throw<UnknownCommandException>();
    }

    [Fact]
    public void GetFor_CalledMultipleTimes_ReturnsConsistentExecutor()
    {
        var expectedExecutor = Substitute.For<ICommandExecutor<TestCommand, TestAggregate>>();
        var executors = new ICommandExecutor<TestAggregate>[] { expectedExecutor };
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var result1 = factory.GetFor<TestCommand>();
        var result2 = factory.GetFor<TestCommand>();

        result1.Should().BeSameAs(expectedExecutor);
        result2.Should().BeSameAs(expectedExecutor);
        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void GetFor_WithDifferentCommandTypes_ReturnsCorrectExecutors()
    {
        var executor1 = Substitute.For<ICommandExecutor<TestCommand, TestAggregate>>();
        var executor2 = Substitute.For<ICommandExecutor<OtherTestCommand, TestAggregate>>();
        var executors = new ICommandExecutor<TestAggregate>[] { executor1, executor2 };
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var result1 = factory.GetFor<TestCommand>();
        var result2 = factory.GetFor<OtherTestCommand>();

        result1.Should().BeSameAs(executor1);
        result2.Should().BeSameAs(executor2);
    }

    [Theory]
    [AutoData]
    public void GetFor_WithAutoFixtureData_ReturnsExecutor(Guid aggregateId)
    {
        var expectedExecutor = Substitute.For<ICommandExecutor<TestCommand, TestAggregate>>();
        var executors = new ICommandExecutor<TestAggregate>[] { expectedExecutor };
        var factory = new CommandExecutorFactory<TestAggregate>(executors);

        var result = factory.GetFor<TestCommand>();

        result.Should().NotBeNull();
        result.Should().BeSameAs(expectedExecutor);
        aggregateId.Should().NotBeEmpty();
    }
}

public class TestAggregate : IAggregateRoot
{
    public Guid Id { get; set; }
    public int LastSequence { get; set; }
}

public record TestCommand(Guid Id) : ICommand
{
    public CascadeEsdm.SharedKernel.ValueObjects.ISubject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<TestAggregate>(Id);
    }
}

public record OtherTestCommand : ICommand
{
    public CascadeEsdm.SharedKernel.ValueObjects.ISubject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<TestAggregate>(Guid.NewGuid());
    }
}

public record AnotherTestCommand : ICommand
{
    public CascadeEsdm.SharedKernel.ValueObjects.ISubject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<TestAggregate>(Guid.NewGuid());
    }
}
