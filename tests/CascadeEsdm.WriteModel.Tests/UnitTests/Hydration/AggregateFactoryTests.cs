using AutoFixture;
using AutoFixture.Xunit2;
using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Exceptions;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Reflection;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Hydration;

public class AggregateFactoryTests
{
    private readonly IFixture _fixture;

    public AggregateFactoryTests()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Constructor_WithNullEventApplierFactory_ThrowsArgumentNullException()
    {
        var act = () => new AggregateFactory<TestAggregate>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventApplierFactory");
    }

    [Fact]
    public void Constructor_WithValidEventApplierFactory_CreatesInstance()
    {
        var act = () => new AggregateFactory<TestAggregate>(Substitute.For<IEventApplierFactory<TestAggregate>>());

        act.Should().NotThrow();
    }

    [Fact]
    public void GetAggregator_WithNullSnapshot_CreatesNewAggregate()
    {
        var factory = new AggregateFactory<TestAggregate>(Substitute.For<IEventApplierFactory<TestAggregate>>());
        var events = Array.Empty<IEventEnvelope>();

        var result = factory.GetAggregator(events, null);

        result.Should().NotBeNull();
        result.Should().BeOfType<TestAggregate>();
    }

    [Fact]
    public void GetAggregator_WithSnapshot_UsesProvidedSnapshot()
    {
        var factory = new AggregateFactory<TestAggregate>(Substitute.For<IEventApplierFactory<TestAggregate>>());
        var snapshot = new TestAggregate { Id = Guid.NewGuid(), LastSequence = 10 };
        var events = Array.Empty<IEventEnvelope>();

        var result = factory.GetAggregator(events, snapshot);

        result.Should().BeSameAs(snapshot);
        result.LastSequence.Should().Be(10);
    }

    [Fact]
    public void GetAggregator_WithEmptyEvents_ReturnsAggregate()
    {
        var factory = new AggregateFactory<TestAggregate>(Substitute.For<IEventApplierFactory<TestAggregate>>());
        var events = Array.Empty<IEventEnvelope>();

        var result = factory.GetAggregator(events, null);

        result.Should().NotBeNull();
    }

    [Fact]
    public void GetAggregator_WithEventsAndAggregateApplyMethod_AppliesEventsToAggregate()
    {
        var factory = new AggregateFactory<TestAggregateWithApply>(Substitute.For<IEventApplierFactory<TestAggregateWithApply>>());
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 1);
        var events = new[] { eventEnvelope };

        var result = factory.GetAggregator(events, null);

        result.Should().NotBeNull();
        result.AppliedEvents.Should().Contain(@event);
        result.LastSequence.Should().Be(1);
    }

    [Fact]
    public void GetAggregator_WithMultipleEvents_AppliesAllEventsInOrder()
    {
        var factory = new AggregateFactory<TestAggregateWithApply>(Substitute.For<IEventApplierFactory<TestAggregateWithApply>>());
        var event1 = new TestEvent { Value = "First" };
        var event2 = new TestEvent { Value = "Second" };
        var event3 = new TestEvent { Value = "Third" };
        var events = new[]
        {
            TestTools.CreateEventEnvelope(event1, 1),
            TestTools.CreateEventEnvelope(event2, 2),
            TestTools.CreateEventEnvelope(event3, 3)
        };

        var result = factory.GetAggregator(events, null);

        result.AppliedEvents.Should().HaveCount(3);
        result.AppliedEvents[0].Should().Be(event1);
        result.AppliedEvents[1].Should().Be(event2);
        result.AppliedEvents[2].Should().Be(event3);
        result.LastSequence.Should().Be(3);
    }

    [Fact]
    public void GetAggregator_WithApplyMethodTakingEventAndEnvelope_PassesBothParameters()
    {
        var factory = new AggregateFactory<TestAggregateWithApplyAndEnvelope>(Substitute.For<IEventApplierFactory<TestAggregateWithApplyAndEnvelope>>());
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 5);
        var events = new[] { eventEnvelope };

        var result = factory.GetAggregator(events, null);

        result.AppliedEvents.Should().Contain(@event);
        result.ReceivedEnvelopes.Should().Contain(eventEnvelope);
        result.LastSequence.Should().Be(5);
    }

    [Fact]
    public void GetAggregator_WithNoMatchingApplyMethod_UsesEventApplier()
    {
        var mockApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();

        var mockApplierFactory = Substitute.For<IEventApplierFactory<TestAggregate>>();
        mockApplierFactory.GetFor<TestEvent>().Returns(mockApplier);
        
        var factory = new AggregateFactory<TestAggregate>(mockApplierFactory);
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 1);
        var events = new[] { eventEnvelope };

        var result = factory.GetAggregator(events, null);

        mockApplier.Received(1).Apply(Arg.Any<TestAggregate>(), @event, eventEnvelope);
        result.LastSequence.Should().Be(1);
    }

    [Fact]
    public void GetAggregator_WhenApplyMethodThrowsExceptionBase_RethrowsException()
    {
        var factory = new AggregateFactory<TestAggregateWithThrowingApply>(Substitute.For<IEventApplierFactory<TestAggregateWithThrowingApply>>());
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 1);
        var events = new[] { eventEnvelope };

        var act = () => factory.GetAggregator(events, null);

        act.Should().Throw<TestExceptionBase>();
    }

    [Fact]
    public void GetAggregator_WhenApplyMethodThrowsGenericException_WrapsInEventHydrationException()
    {
        var factory = new AggregateFactory<TestAggregateWithFailingApply>(Substitute.For<IEventApplierFactory<TestAggregateWithFailingApply>>());
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 1);
        var events = new[] { eventEnvelope };

        var x = Assert.Throws<EventHydrationException>(() => factory.GetAggregator(events, null));

        x.Message.Should().Contain(nameof(TestEvent))
            .And.Contain(nameof(TestAggregateWithFailingApply));
        
        x.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void GetAggregator_WhenEventApplierThrowsExceptionBase_RethrowsException()
    {
        var mockApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var expectedException = new TestExceptionBase();
        mockApplier.When(x => x.Apply(Arg.Any<TestAggregate>(), Arg.Any<TestEvent>(), Arg.Any<IEventEnvelope>()))
            .Do(_ => throw expectedException);
        
        var mockApplierFactory = Substitute.For<IEventApplierFactory<TestAggregate>>();
        mockApplierFactory.GetFor<TestEvent>().Returns(mockApplier);
        
        var factory = new AggregateFactory<TestAggregate>(mockApplierFactory);
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 1);
        var events = new[] { eventEnvelope };

        var act = () => factory.GetAggregator(events, null);

        act.Should().Throw<TestExceptionBase>()
            .Which.Should().BeSameAs(expectedException);
    }

    [Fact]
    public void GetAggregator_WhenEventApplierThrowsGenericException_WrapsInEventHydrationException()
    {
        var mockApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var innerException = new InvalidOperationException("Applier failed");
        mockApplier.When(x => x.Apply(Arg.Any<TestAggregate>(), Arg.Any<TestEvent>(), Arg.Any<IEventEnvelope>()))
            .Do(_ => throw innerException);
        
        var mockApplierFactory = Substitute.For<IEventApplierFactory<TestAggregate>>();
        mockApplierFactory.GetFor<TestEvent>().Returns(mockApplier);
        
        var factory = new AggregateFactory<TestAggregate>(mockApplierFactory);
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 1);
        var events = new[] { eventEnvelope };

        var act = () => factory.GetAggregator(events, null);

        act.Should().Throw<EventHydrationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Applier failed");
    }

    [Fact]
    public void GetAggregator_UpdatesLastSequenceForEachEvent()
    {
        var factory = new AggregateFactory<TestAggregateWithApply>(Substitute.For<IEventApplierFactory<TestAggregateWithApply>>());
        var events = new[]
        {
            TestTools.CreateEventEnvelope(new TestEvent { Value = "1" }, 5),
            TestTools.CreateEventEnvelope(new TestEvent { Value = "2" }, 10),
            TestTools.CreateEventEnvelope(new TestEvent { Value = "3" }, 15)
        };

        var result = factory.GetAggregator(events, null);

        result.LastSequence.Should().Be(15);
    }

    [Fact]
    public void GetAggregator_WithSnapshotAndEvents_AppliesEventsToSnapshot()
    {
        var factory = new AggregateFactory<TestAggregateWithApply>(Substitute.For<IEventApplierFactory<TestAggregateWithApply>>());
        var snapshot = new TestAggregateWithApply 
        { 
            Id = Guid.NewGuid(), 
            LastSequence = 5 
        };
        snapshot.AppliedEvents.Add(new TestEvent { Value = "Snapshot" });
        
        var @event = new TestEvent { Value = "New" };
        var events = new[] { TestTools.CreateEventEnvelope(@event, 6) };

        var result = factory.GetAggregator(events, snapshot);

        result.Should().BeSameAs(snapshot);
        result.AppliedEvents.Should().HaveCount(2);
        result.AppliedEvents[0].Value.Should().Be("Snapshot");
        result.AppliedEvents[1].Value.Should().Be("New");
        result.LastSequence.Should().Be(6);
    }

    [Fact]
    public void GetAggregator_WithMixedEventTypes_AppliesAllCorrectly()
    {
        var mockApplier = Substitute.For<IEventApplier<OtherTestEvent, TestAggregateWithApply>>();
        var mockApplierFactory = Substitute.For<IEventApplierFactory<TestAggregateWithApply>>();
        mockApplierFactory.GetFor<OtherTestEvent>().Returns(mockApplier);
        
        var factory = new AggregateFactory<TestAggregateWithApply>(mockApplierFactory);
        var event1 = new TestEvent { Value = "First" };
        var event2 = new OtherTestEvent { Data = 42 };
        var event3 = new TestEvent { Value = "Third" };
        var events = new IEventEnvelope[]
        {
            TestTools.CreateEventEnvelope(event1, 1),
            TestTools.CreateEventEnvelope(event2, 2),
            TestTools.CreateEventEnvelope(event3, 3)
        };

        var result = factory.GetAggregator(events, null);

        result.AppliedEvents.Should().HaveCount(2);
        result.AppliedEvents[0].Should().Be(event1);
        result.AppliedEvents[1].Should().Be(event3);
        mockApplier.Received(1).Apply(result, event2, Arg.Any<IEventEnvelope>());
        result.LastSequence.Should().Be(3);
    }

    [Theory]
    [AutoData]
    public void GetAggregator_WithAutoFixtureData_ExecutesSuccessfully(Guid aggregateId)
    {
        var factory = new AggregateFactory<TestAggregate>(Substitute.For<IEventApplierFactory<TestAggregate>>());
        var events = Array.Empty<IEventEnvelope>();

        var result = factory.GetAggregator(events, null);

        result.Should().NotBeNull();
        aggregateId.Should().NotBeEmpty();
    }

    [Fact]
    public void GetAggregator_WhenEventApplierFactoryThrowsException_UnwrapsTargetInvocationException()
    {
        var mockFactory = Substitute.For<IEventApplierFactory<TestAggregate>>();
        var innerException = new InvalidOperationException("Factory error");
        mockFactory.GetFor<TestEvent>().Throws(innerException);
        
        var factory = new AggregateFactory<TestAggregate>(mockFactory);
        var @event = new TestEvent { Value = "Test" };
        var eventEnvelope = TestTools.CreateEventEnvelope(@event, 1);
        var events = new[] { eventEnvelope };

        var x = Assert.Throws<EventHydrationException>(() => factory.GetAggregator(events, null));

        x.InnerException.Should().BeOfType<InvalidOperationException>();
    }
}

public class TestAggregate : IAggregateRoot
{
    public Guid Id { get; set; }
    public int LastSequence { get; set; }
}

public class TestAggregateWithApply : IAggregateRoot
{
    public Guid Id { get; set; }
    public int LastSequence { get; set; }
    public List<TestEvent> AppliedEvents { get; } = new();

    public void Apply(TestEvent @event)
    {
        AppliedEvents.Add(@event);
    }
}

public class TestAggregateWithApplyAndEnvelope : IAggregateRoot
{
    public Guid Id { get; set; }
    public int LastSequence { get; set; }
    public List<TestEvent> AppliedEvents { get; } = new();
    public List<IEventEnvelope> ReceivedEnvelopes { get; } = new();

    public void Apply(TestEvent @event, IEventEnvelope envelope)
    {
        AppliedEvents.Add(@event);
        ReceivedEnvelopes.Add(envelope);
    }
}

public class TestAggregateWithThrowingApply : IAggregateRoot
{
    public Guid Id { get; set; }
    public int LastSequence { get; set; }

    public void Apply(TestEvent @event)
    {
        throw new TestExceptionBase();
    }
}

public class TestAggregateWithFailingApply : IAggregateRoot
{
    public Guid Id { get; set; }
    public int LastSequence { get; set; }

    public void Apply(TestEvent @event)
    {
        throw new InvalidOperationException("Apply failed");
    }
}

public record TestEvent : IDomainEvent
{
    public string Value { get; set; } = string.Empty;
}

public record OtherTestEvent : IDomainEvent
{
    public int Data { get; set; }
}

public class TestExceptionBase : ExceptionBase
{
    public TestExceptionBase() : base("Test exception")
    {
    }
}
