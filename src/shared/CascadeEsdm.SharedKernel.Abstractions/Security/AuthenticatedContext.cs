using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Security;

public record AuthenticatedContext
{
    public AuthenticatedContext(UserIdentity user, Tenant tenant)
    {
        User = user;
        Tenant = tenant;
    }

    public UserIdentity User { get; }
    public Tenant Tenant { get; }
}