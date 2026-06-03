using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     A request for a single view row by id, scoped to an authenticated context and a parent partition.
/// </summary>
public abstract record ScopedSingleQuery
{
    protected ScopedSingleQuery(Guid id, AuthenticatedContext securityContext)
    {
        Id = id;
        SecurityContext = securityContext;
    }

    public Guid Id { get; }
    public AuthenticatedContext SecurityContext { get; }

    /// <summary>
    ///     The identifier of the parent the row is scoped to, used to resolve the storage partition.
    /// </summary>
    public abstract Guid? GetParentId();
}
