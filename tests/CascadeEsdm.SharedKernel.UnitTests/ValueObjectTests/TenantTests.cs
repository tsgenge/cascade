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
    public void Constructor_WithEmptyGuid_CreatesValidTenant()
    {
        var tenant = new Tenant(Guid.Empty);

        tenant.Value.Should().Be(Guid.Empty);
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
    public void ImplicitOperator_CanBeUsedInExpressions()
    {
        var id = Guid.NewGuid();
        var tenant = new Tenant(id);

        var matches = id == tenant;

        matches.Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_SameValue_AreEqual()
    {
        var id = Guid.NewGuid();
        var tenant1 = new Tenant(id);
        var tenant2 = new Tenant(id);

        tenant1.Should().Be(tenant2);
        (tenant1 == tenant2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var tenant1 = new Tenant(Guid.NewGuid());
        var tenant2 = new Tenant(Guid.NewGuid());

        tenant1.Should().NotBe(tenant2);
        (tenant1 != tenant2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        var id = Guid.NewGuid();
        var tenant1 = new Tenant(id);
        var tenant2 = new Tenant(id);

        tenant1.GetHashCode().Should().Be(tenant2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ReturnsDifferentHash()
    {
        var tenant1 = new Tenant(Guid.NewGuid());
        var tenant2 = new Tenant(Guid.NewGuid());

        tenant1.GetHashCode().Should().NotBe(tenant2.GetHashCode());
    }

    [Fact]
    public void ImplementsIValueObject_Interface()
    {
        var tenant = new Tenant(Guid.NewGuid());

        tenant.Should().BeAssignableTo<IValueObject>();
        tenant.Should().BeAssignableTo<IValueObject<Guid>>();
    }
}
