using AutoFixture;
using AutoFixture.Xunit2;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Hydration;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Hydration;

public class AggregateHydratorTests
{
    private readonly IFixture _fixture;
    private readonly IAggregateFactory<TestAggregate> _mockAggregateFactory;
    private readonly AuthenticatedContext _mockContext;
    private readonly ISnapshotReader<TestAggregate> _mockSnapshotReader;
    private readonly IEventStreamReader _mockStreamReader;

    public AggregateHydratorTests()
    {
        _fixture = new Fixture();
        _mockStreamReader = Substitute.For<IEventStreamReader>();
        _mockAggregateFactory = Substitute.For<IAggregateFactory<TestAggregate>>();
        _mockSnapshotReader = Substitute.For<ISnapshotReader<TestAggregate>>();
        _mockContext = Substitute.For<AuthenticatedContext>();
    }

    [Fact]
    public void Constructor_WithNullStreamReader_ThrowsArgumentNullException()
    {
        var act = () => new AggregateHydrator<TestAggregate>(
            null!,
            _mockAggregateFactory,
            _mockSnapshotReader);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("streamReader");
    }

    [Fact]
    public void Constructor_WithNullAggregateFactory_ThrowsArgumentNullException()
    {
        var act = () => new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            null!,
            _mockSnapshotReader);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("aggregateFactory");
    }

    [Fact]
    public void Constructor_WithNullSnapshotReader_ThrowsArgumentNullException()
    {
        var act = () => new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snapshotReader");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var act = () => new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task HydrateAsync_ReadsLatestSnapshot()
    {
        var subjectId = Guid.NewGuid();
        var events = new List<EventEnvelope>();
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, _mockContext);

        await _mockSnapshotReader.Received(1).GetLatestAsync(subjectId);
    }

    [Fact]
    public async Task HydrateAsync_ReadsAllEventsForAggregate()
    {
        var subjectId = Guid.NewGuid();
        var events = new List<EventEnvelope>();
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, _mockContext);

        await _mockStreamReader.Received(1).ReadAllAsync<TestAggregate>(subjectId);
    }

    [Fact]
    public async Task HydrateAsync_CallsAggregateFactoryWithEventsAndSnapshot()
    {
        var subjectId = Guid.NewGuid();
        var events = new List<EventEnvelope> { TestTools.CreateEventEnvelope(), TestTools.CreateEventEnvelope() };
        var snapshot = new TestAggregate { Id = subjectId, LastSequence = 5 };
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(snapshot);
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, snapshot).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, _mockContext);

        _mockAggregateFactory.Received(1).GetAggregator(events, snapshot);
    }

    [Fact]
    public async Task HydrateAsync_ReturnsAggregateFromFactory()
    {
        var subjectId = Guid.NewGuid();
        var events = new List<EventEnvelope>();
        var expectedAggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(expectedAggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        var result = await hydrator.HydrateAsync(subjectId, _mockContext);

        result.Should().BeSameAs(expectedAggregate);
    }

    [Fact]
    public async Task HydrateAsync_WithNoSnapshot_PassesNullToFactory()
    {
        var subjectId = Guid.NewGuid();
        var events = new List<EventEnvelope>();
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, _mockContext);

        _mockAggregateFactory.Received(1).GetAggregator(events, Arg.Is<TestAggregate?>(x => x == null));
    }

    [Fact]
    public async Task HydrateAsync_WhenFactoryThrowsException_WrapsInException()
    {
        var subjectId = Guid.NewGuid();
        var events = new List<EventEnvelope>();
        var innerException = new InvalidOperationException("Factory error");

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Throws(innerException);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        var act = async () => await hydrator.HydrateAsync(subjectId, _mockContext);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"Unable to instance the aggregate for hydration ({typeof(TestAggregate).Name}).")
            .WithInnerException(typeof(InvalidOperationException));
    }

    [Fact]
    public async Task HydrateAsync_WithFromSequenceId_ReadsSnapshotWithSequenceId()
    {
        var subjectId = Guid.NewGuid();
        var fromSequenceId = 10;
        var events = new List<EventEnvelope>();
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId, fromSequenceId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, fromSequenceId, _mockContext);

        await _mockSnapshotReader.Received(1).GetLatestAsync(subjectId, fromSequenceId);
    }

    [Fact]
    public async Task HydrateAsync_WithFromSequenceId_ReadsAllEventsForAggregate()
    {
        var subjectId = Guid.NewGuid();
        var fromSequenceId = 10;
        var events = new List<EventEnvelope>();
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId, fromSequenceId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, fromSequenceId, _mockContext);

        await _mockStreamReader.Received(1).ReadAllAsync<TestAggregate>(subjectId);
    }

    [Fact]
    public async Task HydrateAsync_WithFromSequenceId_CallsAggregateFactoryWithEventsAndSnapshot()
    {
        var subjectId = Guid.NewGuid();
        var fromSequenceId = 10;
        var events = new List<EventEnvelope> { TestTools.CreateEventEnvelope() };
        var snapshot = new TestAggregate { Id = subjectId, LastSequence = 8 };
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId, fromSequenceId).Returns(snapshot);
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, snapshot).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, fromSequenceId, _mockContext);

        _mockAggregateFactory.Received(1).GetAggregator(events, snapshot);
    }

    [Fact]
    public async Task HydrateAsync_WithFromSequenceId_ReturnsAggregateFromFactory()
    {
        var subjectId = Guid.NewGuid();
        var fromSequenceId = 10;
        var events = new List<EventEnvelope>();
        var expectedAggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId, fromSequenceId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(expectedAggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        var result = await hydrator.HydrateAsync(subjectId, fromSequenceId, _mockContext);

        result.Should().BeSameAs(expectedAggregate);
    }

    [Fact]
    public async Task HydrateAsync_WithFromSequenceId_WhenFactoryThrowsException_WrapsInException()
    {
        var subjectId = Guid.NewGuid();
        var fromSequenceId = 10;
        var events = new List<EventEnvelope>();
        var innerException = new InvalidOperationException("Factory error");

        _mockSnapshotReader.GetLatestAsync(subjectId, fromSequenceId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Throws(innerException);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        var act = async () => await hydrator.HydrateAsync(subjectId, fromSequenceId, _mockContext);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"Unable to instance the aggregate for hydration ({typeof(TestAggregate).Name}).")
            .WithInnerException(typeof(InvalidOperationException));
    }

    [Fact]
    public async Task HydrateAsync_WithMultipleEvents_PassesAllEventsToFactory()
    {
        var subjectId = Guid.NewGuid();
        var events = new List<EventEnvelope>
        {
            TestTools.CreateEventEnvelope(), TestTools.CreateEventEnvelope(), TestTools.CreateEventEnvelope()
        };
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        await hydrator.HydrateAsync(subjectId, _mockContext);

        _mockAggregateFactory.Received(1).GetAggregator(
            Arg.Is<IEnumerable<EventEnvelope>>(e => e.Count() == 3),
            Arg.Any<TestAggregate?>());
    }

    [Theory]
    [AutoData]
    public async Task HydrateAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid subjectId)
    {
        var events = new List<EventEnvelope>();
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        var result = await hydrator.HydrateAsync(subjectId, _mockContext);

        result.Should().NotBeNull();
        result.Id.Should().Be(subjectId);
    }

    [Theory]
    [AutoData]
    public async Task HydrateAsync_WithFromSequenceId_WithAutoFixtureData_ExecutesSuccessfully(Guid subjectId,
        int fromSequenceId)
    {
        var events = new List<EventEnvelope>();
        var aggregate = new TestAggregate { Id = subjectId };

        _mockSnapshotReader.GetLatestAsync(subjectId, fromSequenceId).Returns(Task.FromResult<TestAggregate?>(null));
        _mockStreamReader.ReadAllAsync<TestAggregate>(subjectId).Returns(events);
        _mockAggregateFactory.GetAggregator(events, null).Returns(aggregate);

        var hydrator = new AggregateHydrator<TestAggregate>(
            _mockStreamReader,
            _mockAggregateFactory,
            _mockSnapshotReader);

        var result = await hydrator.HydrateAsync(subjectId, fromSequenceId, _mockContext);

        result.Should().NotBeNull();
        result.Id.Should().Be(subjectId);
    }
}