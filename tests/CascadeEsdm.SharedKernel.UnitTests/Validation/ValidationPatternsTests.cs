using CascadeEsdm.SharedKernel.Validation;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Validation;

public class ValidationPatternsTests
{
    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("550e8400e29b41d4a716446655440000")]
    [InlineData("{550e8400-e29b-41d4-a716-446655440000}")]
    [InlineData("(550e8400-e29b-41d4-a716-446655440000)")]
    [InlineData("550E8400-E29B-41D4-A716-446655440000")]
    public void IsGuid_WithValidGuid_ReturnsTrue(string value)
    {
        ValidationPatterns.IsGuid(value).Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("550e8400-e29b-41d4-a716")]
    [InlineData("zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz")]
    public void IsGuid_WithInvalidValue_ReturnsFalse(string value)
    {
        ValidationPatterns.IsGuid(value).Should().BeFalse();
    }

    [Fact]
    public void GuidPattern_IsNotNull()
    {
        ValidationPatterns.GuidPattern.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Base64Pattern_IsNotNull()
    {
        ValidationPatterns.Base64.Should().NotBeNullOrEmpty();
    }
}
