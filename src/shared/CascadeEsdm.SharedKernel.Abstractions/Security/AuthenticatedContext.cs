using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Security;

public record AuthenticatedContext : IAuthenticatedContext
{
    public AuthenticatedContext(UserIdentity user, Tenant tenant)
    {
        User = user;
        Tenant = tenant;
    }

    public IUserIdentity User { get; }
    public ITenant Tenant { get; }
}