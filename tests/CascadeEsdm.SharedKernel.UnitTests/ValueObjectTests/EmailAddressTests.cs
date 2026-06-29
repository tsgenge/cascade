using System.ComponentModel.DataAnnotations;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.ValueObjectTests;

public class EmailAddressTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user-name@example.co.uk")]
    [InlineData("user_name@sub.example.com")]
    [InlineData("u@example.io")]
    public void Constructor_WithValidEmail_SetsValue(string email)
    {
        var emailAddress = new EmailAddress(email);

        emailAddress.Value.Should().Be(email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@nodomain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData("")]
    public void Constructor_WithInvalidEmail_ThrowsValidationException(string email)
    {
        Action act = () => new EmailAddress(email);

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Pattern_MatchesValidEmail()
    {
        var result = System.Text.RegularExpressions.Regex.IsMatch("user@example.com", EmailAddress.Pattern);

        result.Should().BeTrue();
    }

    [Fact]
    public void Pattern_DoesNotMatchInvalidEmail()
    {
        var result = System.Text.RegularExpressions.Regex.IsMatch("not-an-email", EmailAddress.Pattern);

        result.Should().BeFalse();
    }

    [Fact]
    public void TwoEmailAddresses_WithSameValue_AreEqual()
    {
        var a = new EmailAddress("user@example.com");
        var b = new EmailAddress("user@example.com");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoEmailAddresses_WithDifferentValues_AreNotEqual()
    {
        var a = new EmailAddress("user@example.com");
        var b = new EmailAddress("other@example.com");

        a.Should().NotBe(b);
    }
}
