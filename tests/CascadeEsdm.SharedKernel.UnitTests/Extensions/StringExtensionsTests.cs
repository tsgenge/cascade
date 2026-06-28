using CascadeEsdm.SharedKernel.Extensions;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void ToGuid_WithValue_ReturnsDeterministicGuid()
    {
        var result = "test-value".ToGuid();

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ToGuid_SameInput_ReturnsSameGuid()
    {
        var result1 = "test-value".ToGuid();
        var result2 = "test-value".ToGuid();

        result1.Should().Be(result2);
    }

    [Fact]
    public void ToGuid_DifferentInputs_ReturnDifferentGuids()
    {
        var result1 = "value-one".ToGuid();
        var result2 = "value-two".ToGuid();

        result1.Should().NotBe(result2);
    }

    [Fact]
    public void ToGuid_EmptyString_ReturnsEmptyGuid()
    {
        var result = "".ToGuid();

        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ToGuid_Whitespace_ReturnsEmptyGuid()
    {
        var result = "   ".ToGuid();

        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ToGuid_Null_ReturnsEmptyGuid()
    {
        string? value = null;

        var result = value!.ToGuid();

        result.Should().Be(Guid.Empty);
    }
}
