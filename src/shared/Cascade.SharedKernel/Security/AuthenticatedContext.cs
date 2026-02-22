using Cascade.SharedKernel.ValueObjects;

namespace Cascade.SharedKernel.Security;

public record AuthenticatedContext : IAuthenticatedContext
{
    public AuthenticatedContext(UserIdentity user)
    {
        User = user;
    }

    public IUserIdentity User { get; }
}