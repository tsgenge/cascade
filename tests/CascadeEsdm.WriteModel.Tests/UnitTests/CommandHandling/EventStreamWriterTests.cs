using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class EventStreamWriterTests
{
    private readonly IPartitionedContainer<TestContainerDefinition> _mockContainer;
    private readonly IAggregatePartitionLocator _mockPartitionLocator;

    public EventStreamWriterTests()
    {
        _mockContainer = Substitute.For<IPartitionedContainer<TestContainerDefinition>>();
        _mockPartitionLocator = Substitute.For<IAggregatePartitionLocator>();
    }

    [Fact]
    public void Constructor_WithNullContainer_ThrowsArgumentNullException()
    {
        var act = () => new EventStreamWriter<TestContainerDefinition>(null!, _mockPartitionLocator);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("container");
    }

    [Fact]
    public void Constructor_WithNullPartitionLocator_ThrowsArgumentNullException()
    {
        var act = () => new EventStreamWriter<TestContainerDefinition>(_mockContainer, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("partitionLocator");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var act = () => new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Added_Events_Are_Saved_To_Container()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var eventEnvelope = TestTools.CreateEventEnvelope();

        writer.Add(eventEnvelope);

        // Verify by calling SaveAsync and checking the batch contains our event
        _mockPartitionLocator.GetPartition(eventEnvelope.Subject).Returns("test-partition");

        await writer.SaveAsync();

        await _mockContainer.Received(1).AddBatchAsync(Arg.Is<IList<EventDocument>>(
            docs => docs.Count == 1 && docs[0].Envelope == eventEnvelope));
    }

    [Fact]
    public async Task SaveAsync_WithNoEvents_DoesNotCallContainer()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);

        await writer.SaveAsync();

        await _mockContainer.DidNotReceive().AddBatchAsync(Arg.Any<IList<EventDocument>>());
    }

    [Fact]
    public async Task SaveAsync_WithEvents_GetsPartitionFromFirstEventSubject()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var firstEvent = TestTools.CreateEventEnvelope();
        var secondEvent = TestTools.CreateEventEnvelope();

        _mockPartitionLocator.GetPartition(firstEvent.Subject).Returns("test-partition");

        writer.Add(firstEvent);
        writer.Add(secondEvent);
        await writer.SaveAsync();

        _mockPartitionLocator.Received(1).GetPartition(firstEvent.Subject);
    }

    [Fact]
    public async Task SaveAsync_WithEvents_CallsAddBatchAsyncWithCorrectDocuments()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var event1 = TestTools.CreateEventEnvelope();
        var event2 = TestTools.CreateEventEnvelope();
        var partition = "test-partition";

        _mockPartitionLocator.GetPartition(event1.Subject).Returns(partition);

        writer.Add(event1);
        writer.Add(event2);
        await writer.SaveAsync();

        await _mockContainer.Received(1).AddBatchAsync(Arg.Is<IList<EventDocument>>(docs =>
            docs.Count == 2 &&
            docs[0].Id == event1.Id &&
            docs[0].PartitionKey == partition &&
            docs[0].Envelope == event1 &&
            docs[1].Id == event2.Id &&
            docs[1].PartitionKey == partition &&
            docs[1].Envelope == event2));
    }

    [Fact]
    public async Task SaveAsync_ClearsEventsAfterSuccessfulSave()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var eventEnvelope = TestTools.CreateEventEnvelope();

        _mockPartitionLocator.GetPartition(eventEnvelope.Subject).Returns("test-partition");

        writer.Add(eventEnvelope);
        await writer.SaveAsync();

        // Verify events are cleared by calling SaveAsync again
        await writer.SaveAsync();

        await _mockContainer.Received(1).AddBatchAsync(Arg.Any<IList<EventDocument>>());
    }

    [Fact]
    public async Task SaveAsync_WhenContainerThrowsException_WrapsInEventWritingException()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var eventEnvelope = TestTools.CreateEventEnvelope();
        var innerException = new InvalidOperationException("Database error");

        _mockPartitionLocator.GetPartition(eventEnvelope.Subject).Returns("test-partition");
        _mockContainer.AddBatchAsync(Arg.Any<IList<EventDocument>>())
            .Throws(innerException);

        writer.Add(eventEnvelope);

        var ex = await Assert.ThrowsAsync<EventWritingException>(async () => await writer.SaveAsync());
        ex.InnerException.Should().Be(innerException);
    }

    [Fact]
    public async Task SaveAsync_WhenContainerThrowsException_ClearsEventsInFinallyBlock()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var eventEnvelope = TestTools.CreateEventEnvelope();

        _mockPartitionLocator.GetPartition(eventEnvelope.Subject).Returns("test-partition");
        _mockContainer.AddBatchAsync(Arg.Any<IList<EventDocument>>())
            .Throws(new InvalidOperationException("Database error"));

        writer.Add(eventEnvelope);

        try
        {
            await writer.SaveAsync();
        }
        catch (EventWritingException)
        {
            // Expected
        }

        // Verify events are cleared by checking no additional calls with the same event
        _mockContainer.ClearReceivedCalls();

        // Second SaveAsync should not call container because events were cleared
        await writer.SaveAsync();

        await _mockContainer.DidNotReceive().AddBatchAsync(Arg.Any<IList<EventDocument>>());
    }

    [Fact]
    public async Task SaveAsync_WithMultipleEvents_UsesSamePartitionForAllDocuments()
    {
        var writer = new EventStreamWriter<TestContainerDefinition>(_mockContainer, _mockPartitionLocator);
        var events = new List<EventEnvelope>
        {
            TestTools.CreateEventEnvelope(),
            TestTools.CreateEventEnvelope(),
            TestTools.CreateEventEnvelope()
        };
        var partition = "shared-partition";

        _mockPartitionLocator.GetPartition(events[0].Subject).Returns(partition);

        foreach (var evt in events)
        {
            writer.Add(evt);
        }

        await writer.SaveAsync();

        await _mockContainer.Received(1).AddBatchAsync(Arg.Is<IList<EventDocument>>(
            docs => docs.All(d => d.PartitionKey == partition)));
    }

    public interface ITestContainer : IDocumentContainerDefinition { }

    public class TestContainerDefinition : IDocumentContainerDefinition
    {
        public string Name => "TestContainer";
        public int TimeToLive => -1;
    }
}
