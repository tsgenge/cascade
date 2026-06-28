using CascadeEsdm.SharedKernel.Exceptions;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Exceptions;

public class ExceptionTests
{
    private class TestException : ExceptionBase
    {
        public TestException(string message, Exception inner) : base(message, inner) { }
        public TestException(string message) : base(message) { }
        public TestException(string message, int httpStatus) : base(message, httpStatus) { }
        public TestException(string message, int httpStatus, Exception inner) : base(message, httpStatus, inner) { }
        public TestException(int httpStatus) : base(httpStatus) { }

        public void SetPublicMessage(string msg) => PublicMessage = msg;
        public void SetHttpStatusCode(int code) => HttpStatusCode = code;
    }

    [Fact]
    public void ExceptionBase_MessageAndInner_SetsProperties()
    {
        var inner = new InvalidOperationException("inner");

        var ex = new TestException("test message", inner);

        ex.Message.Should().Be("test message");
        ex.InnerException.Should().Be(inner);
        ex.HttpStatusCode.Should().Be(500);
    }

    [Fact]
    public void ExceptionBase_MessageOnly_SetsMessage()
    {
        var ex = new TestException("test message");

        ex.Message.Should().Be("test message");
        ex.InnerException.Should().BeNull();
        ex.HttpStatusCode.Should().Be(500);
    }

    [Fact]
    public void ExceptionBase_MessageAndHttpStatus_SetsBoth()
    {
        var ex = new TestException("test message", 404);

        ex.Message.Should().Be("test message");
        ex.HttpStatusCode.Should().Be(404);
    }

    [Fact]
    public void ExceptionBase_MessageHttpStatusAndInner_SetsAll()
    {
        var inner = new InvalidOperationException("inner");

        var ex = new TestException("test message", 400, inner);

        ex.Message.Should().Be("test message");
        ex.HttpStatusCode.Should().Be(400);
        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ExceptionBase_HttpStatusOnly_SetsHttpStatus()
    {
        var ex = new TestException(503);

        ex.HttpStatusCode.Should().Be(503);
    }

    [Fact]
    public void ExceptionBase_PublicMessage_DefaultsToNull()
    {
        var ex = new TestException("test");

        ex.PublicMessage.Should().BeNull();
    }

    [Fact]
    public void ExceptionBase_PublicMessage_CanBeSet()
    {
        var ex = new TestException("test");

        ex.SetPublicMessage("public message");

        ex.PublicMessage.Should().Be("public message");
    }

    [Fact]
    public void ExceptionBase_HttpStatusCode_CanBeOverridden()
    {
        var ex = new TestException("test");

        ex.SetHttpStatusCode(418);

        ex.HttpStatusCode.Should().Be(418);
    }
}
