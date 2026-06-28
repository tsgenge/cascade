using CascadeEsdm.SharedKernel.Exceptions;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Exceptions;

public class BadMessageFormatExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var inner = new InvalidOperationException("parse error");
        var entityPath = "orders/topic";
        var sessionId = "session-123";

        var ex = new BadMessageFormatException(entityPath, sessionId, inner);

        ex.EntityPath.Should().Be(entityPath);
        ex.SessionId.Should().Be(sessionId);
        ex.InnerException.Should().Be(inner);
        ex.HttpStatusCode.Should().Be(400);
        ex.Message.Should().Contain("invalid format");
    }

    [Fact]
    public void Constructor_WithNullSessionId_SetsNullSessionId()
    {
        var inner = new InvalidOperationException("parse error");

        var ex = new BadMessageFormatException("orders/topic", null, inner);

        ex.SessionId.Should().BeNull();
    }

    [Fact]
    public void EntityPath_CanBeSet()
    {
        var inner = new InvalidOperationException("parse error");
        var ex = new BadMessageFormatException("original", "session", inner);

        ex.EntityPath = "updated";

        ex.EntityPath.Should().Be("updated");
    }

    [Fact]
    public void SessionId_CanBeSet()
    {
        var inner = new InvalidOperationException("parse error");
        var ex = new BadMessageFormatException("path", "original", inner);

        ex.SessionId = "updated";

        ex.SessionId.Should().Be("updated");
    }
}
