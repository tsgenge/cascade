using CascadeEsdm.SharedKernel.Exceptions;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Exceptions;

public class ConflictExceptionTests
{
    [Fact]
    public void Constructor_SetsMessageAndHttpStatus()
    {
        var ex = new ConflictException("resource conflict");

        ex.Message.Should().Be("resource conflict");
        ex.HttpStatusCode.Should().Be(409);
    }

    [Fact]
    public void Constructor_HasNoInnerException()
    {
        var ex = new ConflictException("conflict");

        ex.InnerException.Should().BeNull();
    }
}
