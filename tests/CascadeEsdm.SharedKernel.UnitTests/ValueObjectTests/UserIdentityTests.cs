using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;
using System.Security.Claims;

namespace CascadeEsdm.SharedKernel.UnitTests.ValueObjectTests;

public class UserIdentityTests
{
    [Fact]
    public void Constructor_FromValidGuidString_SetsValue()
    {
        var id = Guid.NewGuid();

        var identity = new UserIdentity(id.ToString());

        identity.Value.Should().Be(id);
    }

    [Fact]
    public void Constructor_FromValidGuidString_WithBraces_SetsValue()
    {
        var id = Guid.NewGuid();

        var identity = new UserIdentity(id.ToString("B"));

        identity.Value.Should().Be(id);
    }

    [Fact]
    public void Constructor_FromInvalidString_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new UserIdentity("not-a-guid");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_FromGuid_SetsValue()
    {
        var id = Guid.NewGuid();

        var identity = new UserIdentity(id);

        identity.Value.Should().Be(id);
    }

    [Fact]
    public void ToString_ReturnsFormattedGuid()
    {
        var id = Guid.NewGuid();
        var identity = new UserIdentity(id);

        var result = identity.ToString();

        result.Should().Be(id.ToString("n"));
    }

    [Fact]
    public void ToClaim_ReturnsSidClaim()
    {
        var id = Guid.NewGuid();
        var identity = new UserIdentity(id);

        var claim = identity.ToClaim();

        claim.Type.Should().Be(ClaimTypes.Sid);
        claim.Value.Should().Be(id.ToString("n"));
    }

    [Fact]
    public void Exists_WithValidGuid_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var identity = new UserIdentity(id);

        var result = identity.Exists();

        result.Should().BeTrue();
    }

    [Fact]
    public void Exists_WithEmptyGuid_ReturnsFalse()
    {
        var identity = new UserIdentity(Guid.Empty);

        var result = identity.Exists();

        result.Should().BeFalse();
    }

    [Fact]
    public void ImplicitOperator_ToGuid_ReturnsValue()
    {
        var id = Guid.NewGuid();
        var identity = new UserIdentity(id);

        Guid result = identity;

        result.Should().Be(id);
    }

    [Fact]
    public void Constructor_FromGuid_WithEmail_SetsBoth()
    {
        var id = Guid.NewGuid();
        var email = new EmailAddress("user@example.com");

        var identity = new UserIdentity(id, email);

        identity.Value.Should().Be(id);
        identity.Email.Should().Be(email);
    }

    [Fact]
    public void Constructor_FromString_WithEmail_SetsBoth()
    {
        var id = Guid.NewGuid();
        var email = new EmailAddress("user@example.com");

        var identity = new UserIdentity(id.ToString(), email);

        identity.Value.Should().Be(id);
        identity.Email.Should().Be(email);
    }

    [Fact]
    public void Constructor_WithoutEmail_EmailIsNull()
    {
        var identity = new UserIdentity(Guid.NewGuid());

        identity.Email.Should().BeNull();
    }
}
