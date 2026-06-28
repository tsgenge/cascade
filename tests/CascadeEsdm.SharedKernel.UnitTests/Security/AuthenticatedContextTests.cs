using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Security;

public class AuthenticatedContextTests
{
    [Fact]
    public void Constructor_SetsUserAndTenant()
    {
        var user = new UserIdentity(Guid.NewGuid());
        var tenant = new Tenant(Guid.NewGuid());

        var context = new AuthenticatedContext(user, tenant);

        context.User.Should().Be(user);
        context.Tenant.Should().Be(tenant);
    }

    [Fact]
    public void Empty_ReturnsContextWithEmptyGuids()
    {
        var context = AuthenticatedContext.Empty;

        context.User.Value.Should().Be(Guid.Empty);
        context.Tenant.Value.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Empty_ReturnsFreshInstanceEachTime()
    {
        var context1 = AuthenticatedContext.Empty;
        var context2 = AuthenticatedContext.Empty;

        context1.Should().Be(context2);
    }
}
