using Cascade.SharedKernel.ValueObjects;

namespace Cascade.SharedKernel.Security;

public interface IAuthenticatedContext
{
    IUserIdentity User { get; }
}