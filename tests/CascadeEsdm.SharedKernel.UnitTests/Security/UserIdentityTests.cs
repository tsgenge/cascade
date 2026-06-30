using CascadeEsdm.SharedKernel.Security;
using FluentAssertions;
using System.Security.Claims;
using Claim = CascadeEsdm.SharedKernel.Security.Claim;

namespace CascadeEsdm.SharedKernel.UnitTests.Security;

public class UserIdentityTests
{
    [Fact]
    public void Constructor_FromValidGuidString_SetsValue()
    {
        var id = Guid.NewGuid();

        var identity = new UserIdentity(id.ToString());

        identity.Id.Should().Be(id);
    }

    [Fact]
    public void Constructor_FromValidGuidString_WithBraces_SetsValue()
    {
        var id = Guid.NewGuid();

        var identity = new UserIdentity(id.ToString("B"));

        identity.Id.Should().Be(id);
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

        identity.Id.Should().Be(id);
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
    public void Constructor_WithClaims_SetsClaims()
    {
        var id = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.Role, "admin") };

        var identity = new UserIdentity(id, claims);

        identity.Claims.Should().HaveCount(1);
        identity.Claims.Single().Type.Should().Be(ClaimTypes.Role);
        identity.Claims.Single().Value.Should().Be("admin");
    }

    [Fact]
    public void Constructor_WithoutClaims_SetsEmptyCollection()
    {
        var id = Guid.NewGuid();

        var identity = new UserIdentity(id);

        identity.Claims.Should().BeEmpty();
    }

    [Fact]
    public void Claims_AreReadOnly()
    {
        var id = Guid.NewGuid();
        var identity = new UserIdentity(id, new[] { new Claim(ClaimTypes.Role, "admin") });

        identity.Claims.Should().BeAssignableTo<IReadOnlyCollection<Claim>>();
    }

    [Fact]
    public void ToClaimsIdentity_IncludesSidAndAllClaims()
    {
        var id = Guid.NewGuid();
        var identity = new UserIdentity(id, new[] { new Claim(ClaimTypes.Role, "admin") });

        var claimsIdentity = identity.ToClaimsIdentity();

        claimsIdentity.Claims.Should().Contain(c => c.Type == ClaimTypes.Sid && c.Value == id.ToString("n"));
        claimsIdentity.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "admin");
    }
}
