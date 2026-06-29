using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Hydration;
using FluentAssertions;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class EventStreamReaderTests
{
    private readonly IPagedContainer<TestContainerDefinition> _mockContainer;
    private readonly IAggregatePartitionLocator _mockPartitionLocator;

    public EventStreamReaderTests()
    {
        _mockContainer = Substitute.For<IPagedContainer<TestContainerDefinition>>();
        _mockPartitionLocator = Substitute.For<IAggregatePartitionLocator>();
    }

    [Fact]
    public void Constructor_WithNullContainer_ThrowsArgumentNullException()
    {
        var act = () => new EventStreamReader<TestContainerDefinition>(null!, _mockPartitionLocator);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("container");
    }

    [Fact]
    public void Constructor_WithNullPartitionLocator_ThrowsArgumentNullException()
    {
        var act = () => new EventStreamReader<TestContainerDefinition>(_mockContainer, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("partitionLocator");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var act = () => new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task ReadAllAsync_GetsPartitionUsingAggregateTypeAndId()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();
        var partition = "test-partition";

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns(partition);
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument>(),
                new PageContinuationToken(null)));

        await reader.ReadAllAsync<TestAggregate>(aggregateId);

        _mockPartitionLocator.Received(1)
            .GetPartition(Arg.Is<Subject>(s => s.Id == aggregateId && s.Type == typeof(TestAggregate).Name));
    }

    [Fact]
    public async Task ReadAllAsync_QueriesContainerWithCorrectPartitionKey()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();
        var partition = "test-partition";

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns(partition);
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument>(),
                new PageContinuationToken(null)));

        await reader.ReadAllAsync<TestAggregate>(aggregateId);

        await _mockContainer.Received(1)
            .GetPageAsync<EventDocument>(Arg.Is<PartitionedPageQuery>(q => q.PartitionKey == partition));
    }

    [Fact]
    public async Task ReadAllAsync_WithEventsInSinglePage_ReturnsAllEvents()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();
        var events = new List<EventEnvelope> { TestTools.CreateEventEnvelope(), TestTools.CreateEventEnvelope() };
        var documents = events.Select(e => new EventDocument(e.Id, "partition", e)).ToList();

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                documents,
                new PageContinuationToken(null)));

        var result = await reader.ReadAllAsync<TestAggregate>(aggregateId);

        result.Should().HaveCount(2);
        result.Should().Contain(events);
    }

    [Fact]
    public async Task ReadAllAsync_WithMultiplePages_FollowsContinuationToken()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();
        var page1Events = new List<EventEnvelope> { TestTools.CreateEventEnvelope() };
        var page2Events = new List<EventEnvelope> { TestTools.CreateEventEnvelope(), TestTools.CreateEventEnvelope() };
        var page1Docs = page1Events.Select(e => new EventDocument(e.Id, "partition", e)).ToList();
        var page2Docs = page2Events.Select(e => new EventDocument(e.Id, "partition", e)).ToList();

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Is<PartitionedPageQuery>(q => q.ContinuationToken == null))
            .Returns(new PageResult<EventDocument>(
                page1Docs,
                new PageContinuationToken("token1")));
        _mockContainer.GetPageAsync<EventDocument>(Arg.Is<PartitionedPageQuery>(q => q.ContinuationToken == "token1"))
            .Returns(new PageResult<EventDocument>(
                page2Docs,
                new PageContinuationToken(null)));

        var result = await reader.ReadAllAsync<TestAggregate>(aggregateId);

        result.Should().HaveCount(3);
        await _mockContainer.Received(2).GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>());
    }

    [Fact]
    public async Task ReadAllAsync_WithEmptyResult_ReturnsEmptyList()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument>(),
                new PageContinuationToken(null)));

        var result = await reader.ReadAllAsync<TestAggregate>(aggregateId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadSingleAsync_GetsPartitionUsingAggregateTypeAndId()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();
        var partition = "test-partition";

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns(partition);
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument>(),
                new PageContinuationToken(null)));

        await reader.ReadSingleAsync<TestAggregate>(aggregateId);

        _mockPartitionLocator.Received(1)
            .GetPartition(Arg.Is<Subject>(s => s.Id == aggregateId && s.Type == typeof(TestAggregate).Name));
    }

    [Fact]
    public async Task ReadSingleAsync_QueriesContainerWithSizeOne()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument>(),
                new PageContinuationToken(null)));

        await reader.ReadSingleAsync<TestAggregate>(aggregateId);

        await _mockContainer.Received(1).GetPageAsync<EventDocument>(Arg.Is<PartitionedPageQuery>(q => q.Size == 1));
    }

    [Fact]
    public async Task ReadSingleAsync_WithEvent_ReturnsEventEnvelope()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();
        var eventEnvelope = TestTools.CreateEventEnvelope();
        var document = new EventDocument(eventEnvelope.Id, "partition", eventEnvelope);

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument> { document },
                new PageContinuationToken(null)));

        var result = await reader.ReadSingleAsync<TestAggregate>(aggregateId);

        result.Should().Be(eventEnvelope);
    }

    [Fact]
    public async Task ReadSingleAsync_WithEmptyResult_ReturnsNull()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument>(),
                new PageContinuationToken(null)));

        var result = await reader.ReadSingleAsync<TestAggregate>(aggregateId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ReadSingleAsync_WithMultipleEvents_ReturnsFirstEventOnly()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();
        var event1 = TestTools.CreateEventEnvelope();
        var event2 = TestTools.CreateEventEnvelope();
        var documents = new List<EventDocument>
        {
            new(event1.Id, "partition", event1), new(event2.Id, "partition", event2)
        };

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                documents,
                new PageContinuationToken(null)));

        var result = await reader.ReadSingleAsync<TestAggregate>(aggregateId);

        result.Should().Be(event1);
    }

    [Fact]
    public async Task ReadAllAsync_WithGenericAggregateType_PassesCorrectTypeName()
    {
        var reader = new EventStreamReader<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var aggregateId = Guid.NewGuid();

        _mockPartitionLocator.GetPartition(Arg.Any<Subject>()).Returns("partition");
        _mockContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                new List<EventDocument>(),
                new PageContinuationToken(null)));

        await reader.ReadAllAsync<AnotherTestAggregate>(aggregateId);

        _mockPartitionLocator.Received(1)
            .GetPartition(Arg.Is<Subject>(s => s.Type == typeof(AnotherTestAggregate).Name));
    }

    public class TestContainerDefinition : IDocumentContainerDefinition
    {
        public string Name => "TestContainer";
        public int TimeToLive => -1;
    }

    private class AnotherTestAggregate : IAggregateRoot
    {
        public Guid Id { get; set; }
        public int LastSequence { get; set; }
    }
}