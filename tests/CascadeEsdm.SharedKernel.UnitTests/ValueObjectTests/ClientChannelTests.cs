using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace CascadeEsdm.SharedKernel.UnitTests.ValueObjectTests;

public class ClientChannelTests
{
    [Theory]
    [InlineData("validChannel12345")]
    [InlineData("Valid-Channel_123")]
    [InlineData("1234567890123456")]
    [InlineData("abcdefghijklmnopqrstuvwxyz123456")]
    public void Constructor_WithValidValue_SetsValue(string validValue)
    {
        var channel = new ClientChannel(validValue);

        channel.Value.Should().Be(validValue);
        channel.Valid.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDuffValue_SetsNA()
    {
        var channel = new ClientChannel("n/a");

        channel.Value.Should().Be("n/a");
        channel.Valid.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithZeroGuidPattern_SetsNA()
    {
        var channel = new ClientChannel("00000000000000000000000000000000/00000000000000000000000000000000");

        channel.Value.Should().Be("n/a");
        channel.Valid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("invalid channel!@#")]
    [InlineData("")]
    [InlineData("toolongvalue1234567890123456789012345678901234567890")]
    public void Constructor_WithInvalidValue_ThrowsValidationException(string invalidValue)
    {
        Action act = () => new ClientChannel(invalidValue);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Empty_ReturnsInvalidChannel()
    {
        var channel = ClientChannel.Empty;

        channel.Value.Should().Be("n/a");
        channel.Valid.Should().BeFalse();
    }

    [Fact]
    public void ParseFromHeader_WithValidHeader_ReturnsChannel()
    {
        var header = "validChannel12345";

        var channel = ClientChannel.ParseFromHeader(header);

        channel.Value.Should().Be(header);
        channel.Valid.Should().BeTrue();
    }

    [Fact]
    public void ParseFromHeader_WithNull_ReturnsEmpty()
    {
        var channel = ClientChannel.ParseFromHeader(null);

        channel.Value.Should().Be("n/a");
        channel.Valid.Should().BeFalse();
    }

    [Fact]
    public void ParseFromHeader_WithWhitespace_ReturnsEmpty()
    {
        var channel = ClientChannel.ParseFromHeader("   ");

        channel.Value.Should().Be("n/a");
        channel.Valid.Should().BeFalse();
    }

    [Fact]
    public void ParseFromHeader_WithEmptyString_ReturnsEmpty()
    {
        var channel = ClientChannel.ParseFromHeader("");

        channel.Value.Should().Be("n/a");
        channel.Valid.Should().BeFalse();
    }
}
