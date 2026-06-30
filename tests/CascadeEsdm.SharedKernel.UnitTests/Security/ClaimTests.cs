using Claim = CascadeEsdm.SharedKernel.Security.Claim;
using SystemClaim = System.Security.Claims.Claim;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using ClaimValueTypes = System.Security.Claims.ClaimValueTypes;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Security;

public class ClaimTests
{
    [Fact]
    public void Constructor_WithTypeAndValue_SetsProperties()
    {
        var claim = new Claim(ClaimTypes.Role, "admin");

        claim.Type.Should().Be(ClaimTypes.Role);
        claim.Value.Should().Be("admin");
        claim.ValueType.Should().Be("http://www.w3.org/2001/XMLSchema#string");
        claim.Issuer.Should().Be("LOCAL AUTHORITY");
        claim.OriginalIssuer.Should().Be("LOCAL AUTHORITY");
    }

    [Fact]
    public void Constructor_WithAllProperties_SetsProperties()
    {
        var claim = new Claim(ClaimTypes.Role, "admin", ClaimValueTypes.String, "issuer", "originalIssuer");

        claim.Type.Should().Be(ClaimTypes.Role);
        claim.Value.Should().Be("admin");
        claim.ValueType.Should().Be(ClaimValueTypes.String);
        claim.Issuer.Should().Be("issuer");
        claim.OriginalIssuer.Should().Be("originalIssuer");
    }

    [Fact]
    public void Constructor_WithNullType_ThrowsArgumentNullException()
    {
        Action act = () => new Claim(null!, "value");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullValue_ThrowsArgumentNullException()
    {
        Action act = () => new Claim(ClaimTypes.Role, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToSystemClaim_ReturnsEquivalentClaim()
    {
        var claim = new Claim(ClaimTypes.Role, "admin", ClaimValueTypes.String, "issuer", "originalIssuer");

        var result = claim.ToSystemClaim();

        result.Type.Should().Be(ClaimTypes.Role);
        result.Value.Should().Be("admin");
        result.ValueType.Should().Be(ClaimValueTypes.String);
        result.Issuer.Should().Be("issuer");
        result.OriginalIssuer.Should().Be("originalIssuer");
    }

    [Fact]
    public void FromSystemClaim_ReturnsEquivalentClaim()
    {
        var systemClaim = new SystemClaim(ClaimTypes.Role, "admin", ClaimValueTypes.String, "issuer", "originalIssuer");

        var result = Claim.FromSystemClaim(systemClaim);

        result.Type.Should().Be(ClaimTypes.Role);
        result.Value.Should().Be("admin");
        result.ValueType.Should().Be(ClaimValueTypes.String);
        result.Issuer.Should().Be("issuer");
        result.OriginalIssuer.Should().Be("originalIssuer");
    }

    [Fact]
    public void FromSystemClaim_WithNullClaim_ThrowsArgumentNullException()
    {
        Action act = () => Claim.FromSystemClaim(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TwoClaims_WithSameValues_AreEqual()
    {
        var a = new Claim(ClaimTypes.Role, "admin");
        var b = new Claim(ClaimTypes.Role, "admin");

        a.Should().Be(b);
    }

    [Fact]
    public void TwoClaims_WithDifferentValues_AreNotEqual()
    {
        var a = new Claim(ClaimTypes.Role, "admin");
        var b = new Claim(ClaimTypes.Role, "user");

        a.Should().NotBe(b);
    }
}
