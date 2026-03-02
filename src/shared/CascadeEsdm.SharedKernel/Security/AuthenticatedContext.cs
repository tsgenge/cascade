using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Security;

public record AuthenticatedContext : IAuthenticatedContext
{
    public AuthenticatedContext(UserIdentity user)
    {
        User = user;
    }

    public IUserIdentity User { get; }
}