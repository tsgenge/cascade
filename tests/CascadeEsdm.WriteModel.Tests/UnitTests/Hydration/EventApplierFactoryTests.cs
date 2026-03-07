using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;
using FluentAssertions;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Hydration;

public class EventApplierFactoryTests
{
    [Fact]
    public void Constructor_WithNullAppliers_ThrowsArgumentNullException()
    {
        var act = () => new EventApplierFactory<TestAggregate>(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("appliers");
    }

    [Fact]
    public void Constructor_WithEmptyAppliers_CreatesInstance()
    {
        var appliers = Array.Empty<IEventApplier<TestAggregate>>();

        var act = () => new EventApplierFactory<TestAggregate>(appliers);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithValidAppliers_CreatesInstance()
    {
        var appliers = new IEventApplier<TestAggregate>[]
        {
            Substitute.For<IEventApplier<TestEvent, TestAggregate>>()
        };

        var act = () => new EventApplierFactory<TestAggregate>(appliers);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetFor_WithMatchingApplier_ReturnsApplier()
    {
        var mockApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] { mockApplier };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var result = factory.GetFor<TestEvent>();

        result.Should().NotBeNull();
        result.Should().BeSameAs(mockApplier);
    }

    [Fact]
    public void GetFor_WithNoMatchingApplier_ThrowsUnknownEventException()
    {
        var appliers = Array.Empty<IEventApplier<TestAggregate>>();
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var act = () => factory.GetFor<TestEvent>();

        act.Should().Throw<UnknownEventException>()
            .Which.Event.Should().Be(nameof(TestEvent));
    }

    [Fact]
    public void GetFor_WithNoMatchingApplier_ThrowsExceptionWithCorrectAggregateSource()
    {
        var appliers = Array.Empty<IEventApplier<TestAggregate>>();
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var act = () => factory.GetFor<TestEvent>();

        act.Should().Throw<UnknownEventException>()
            .Which.AggregateSource.Should().Be(nameof(TestAggregate));
    }

    [Fact]
    public void GetFor_WithMultipleAppliers_ReturnsCorrectApplier()
    {
        var testEventApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var otherEventApplier = Substitute.For<IEventApplier<OtherTestEvent, TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] 
        { 
            testEventApplier,
            otherEventApplier 
        };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var result = factory.GetFor<OtherTestEvent>();

        result.Should().NotBeNull();
        result.Should().BeSameAs(otherEventApplier);
    }

    [Fact]
    public void GetFor_WithMultipleCallsForSameEvent_ReturnsSameApplier()
    {
        var mockApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] { mockApplier };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var result1 = factory.GetFor<TestEvent>();
        var result2 = factory.GetFor<TestEvent>();

        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void GetFor_WithMultipleCallsForDifferentEvents_ReturnsCorrectAppliers()
    {
        var testEventApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var otherEventApplier = Substitute.For<IEventApplier<OtherTestEvent, TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] 
        { 
            testEventApplier,
            otherEventApplier 
        };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var result1 = factory.GetFor<TestEvent>();
        var result2 = factory.GetFor<OtherTestEvent>();

        result1.Should().BeSameAs(testEventApplier);
        result2.Should().BeSameAs(otherEventApplier);
    }

    [Fact]
    public void GetFor_WithWrongEventType_ThrowsUnknownEventException()
    {
        var testEventApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] { testEventApplier };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var act = () => factory.GetFor<OtherTestEvent>();

        act.Should().Throw<UnknownEventException>();
    }

    [Fact]
    public void GetFor_WithNonGenericApplier_DoesNotReturnIt()
    {
        var nonGenericApplier = Substitute.For<IEventApplier<TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] { nonGenericApplier };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var act = () => factory.GetFor<TestEvent>();

        act.Should().Throw<UnknownEventException>();
    }

    [Fact]
    public void GetFor_WithMixedAppliers_ReturnsOnlyMatchingGenericApplier()
    {
        var nonGenericApplier = Substitute.For<IEventApplier<TestAggregate>>();
        var genericApplier = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] 
        { 
            nonGenericApplier,
            genericApplier 
        };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var result = factory.GetFor<TestEvent>();

        result.Should().BeSameAs(genericApplier);
    }

    [Fact]
    public void GetFor_WithDuplicateAppliers_ReturnsFirstMatch()
    {
        var applier1 = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var applier2 = Substitute.For<IEventApplier<TestEvent, TestAggregate>>();
        var appliers = new IEventApplier<TestAggregate>[] 
        { 
            applier1,
            applier2 
        };
        var factory = new EventApplierFactory<TestAggregate>(appliers);

        var result = factory.GetFor<TestEvent>();

        result.Should().BeSameAs(applier1);
    }
}
