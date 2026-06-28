using CascadeEsdm.SharedKernel.Querying;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Querying;

public class PageContinuationTokenTests
{
    [Fact]
    public void Constructor_WithValue_SetsValue()
    {
        var token = new PageContinuationToken("abc123");

        token.Value.Should().Be("abc123");
    }

    [Fact]
    public void Constructor_WithNull_SetsNull()
    {
        var token = new PageContinuationToken(null);

        token.Value.Should().BeNull();
    }

    [Fact]
    public void ImplicitOperator_ToString_ReturnsValue()
    {
        var token = new PageContinuationToken("token-value");

        string? result = token;

        result.Should().Be("token-value");
    }

    [Fact]
    public void ImplicitOperator_WithNull_ReturnsNull()
    {
        var token = new PageContinuationToken(null);

        string? result = token;

        result.Should().BeNull();
    }
}
