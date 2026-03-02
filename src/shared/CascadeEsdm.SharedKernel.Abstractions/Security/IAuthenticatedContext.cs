using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Security;

public interface IAuthenticatedContext
{
    IUserIdentity User { get; }
}