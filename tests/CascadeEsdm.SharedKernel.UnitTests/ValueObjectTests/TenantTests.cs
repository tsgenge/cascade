using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.ValueObjectTests;

public class TenantTests
{
    [Fact]
    public void Constructor_SetsValue()
    {
        var id = Guid.NewGuid();

        var tenant = new Tenant(id);

        tenant.Value.Should().Be(id);
    }

    [Fact]
    public void ImplicitOperator_ToGuid_ReturnsValue()
    {
        var id = Guid.NewGuid();
        var tenant = new Tenant(id);

        Guid result = tenant;

        result.Should().Be(id);
    }

    [Fact]
    public void Tenant_WithEmptyGuid_CreatesValidTenant()
    {
        var id = Guid.Empty;

        var tenant = new Tenant(id);

        tenant.Value.Should().Be(id);
    }
}
