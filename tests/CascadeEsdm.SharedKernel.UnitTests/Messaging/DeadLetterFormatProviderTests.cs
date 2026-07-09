using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Messaging;

public class DeadLetterFormatProviderTests
{
    [Fact]
    public void GetDeadLetterReason_WithNullException_ReturnsEmptyString()
    {
        var result = DeadLetterMessageFormatter.GetDeadLetterReason(null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDeadLetterReason_WithSingleException_ReturnsSingleLine()
    {
        var exception = new InvalidOperationException("something went wrong");

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(exception);

        result.Should().Be("InvalidOperationException: something went wrong");
    }

    [Fact]
    public void GetDeadLetterReason_WithInnerException_ReturnsChainedLine()
    {
        var inner = new ArgumentNullException("value");
        var outer = new InvalidOperationException("outer message", inner);

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(outer);

        result.Should().Be(
            "InvalidOperationException: outer message ---> ArgumentNullException: Value cannot be null. (Parameter 'value')");
    }

    [Fact]
    public void GetDeadLetterReason_WithTwoLevelInnerChain_ReturnsFullChainedLine()
    {
        var root = new NullReferenceException("null ref");
        var middle = new InvalidOperationException("middle", root);
        var outer = new Exception("outer", middle);

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(outer);

        result.Should().Be(
            "Exception: outer ---> InvalidOperationException: middle ---> NullReferenceException: null ref");
    }

    [Fact]
    public void GetDeadLetterReason_WithAggregateExceptionAndSingleChild_ReturnsSingleLine()
    {
        var child = new InvalidOperationException("child error");
        var aggregate = new AggregateException("aggregate message", child);

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(aggregate);

        result.Should().Be(
            "AggregateException: aggregate message (child error) ---> InvalidOperationException: child error");
    }

    [Fact]
    public void GetDeadLetterReason_WithAggregateExceptionAndMultipleChildren_ReturnsOneLinePerChild()
    {
        var child1 = new InvalidOperationException("first error");
        var child2 = new ArgumentException("second error");
        var aggregate = new AggregateException(child1, child2);

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(aggregate);

        var lines = result.Split(Environment.NewLine);
        lines.Should().HaveCount(2);
        lines[0].Should().EndWith("InvalidOperationException: first error");
        lines[1].Should().EndWith("ArgumentException: second error");
    }

    [Fact]
    public void GetDeadLetterReason_WithNestedAggregateException_ReturnsOneLinePerLeaf()
    {
        var leaf1 = new InvalidOperationException("leaf one");
        var leaf2 = new ArgumentNullException("param");
        var leaf3 = new TimeoutException("timed out");

        var innerAggregate = new AggregateException("inner aggregate", leaf1, leaf2);
        var outerAggregate = new AggregateException("outer aggregate", innerAggregate, leaf3);

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(outerAggregate);

        var lines = result.Split(Environment.NewLine);
        lines.Should().HaveCount(3);
        lines[0].Should().Contain("InvalidOperationException: leaf one");
        lines[1].Should().Contain("ArgumentNullException");
        lines[2].Should().Contain("TimeoutException: timed out");
    }

    [Fact]
    public void GetDeadLetterReason_WithAggregateExceptionAndChildWithInnerException_ReturnsFullPathToLeaf()
    {
        var leaf = new NullReferenceException("null ref");
        var child = new InvalidOperationException("child error", leaf);
        var aggregate = new AggregateException("aggregate", child);

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(aggregate);

        result.Should().Contain("AggregateException")
            .And.Contain("InvalidOperationException: child error")
            .And.Contain("NullReferenceException: null ref");
    }

    [Fact]
    public void GetDeadLetterReason_WhenResultExceedsMaxLength_TruncatesToFourThousandAndNinetySixCharacters()
    {
        var longMessage = new string('x', 5000);
        var exception = new InvalidOperationException(longMessage);

        var result = DeadLetterMessageFormatter.GetDeadLetterReason(exception);

        result.Should().HaveLength(4096);
    }
}