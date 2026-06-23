using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     A request for a single view row by id, scoped to an authenticated context and a parent partition.
/// </summary>
public abstract record ScopedSingleQuery<TKey>
    where TKey : IEquatable<TKey>
{
    protected ScopedSingleQuery(TKey id, AuthenticatedContext securityContext)
    {
        Id = id;
        SecurityContext = securityContext;
    }

    public TKey Id { get; }
    public AuthenticatedContext SecurityContext { get; }

    /// <summary>
    ///     The identifier of the parent the row is scoped to, used to resolve the storage partition.
    /// </summary>
    public abstract Guid? GetParentId();
}