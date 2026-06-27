using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.ReadModel.Querying;

public interface IScopedQuery
{
    AuthenticatedContext SecurityContext { get; }
}