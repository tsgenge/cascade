using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.SharedKernel.Security;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     A <see cref="PageFilter" /> scoped to an authenticated context and a parent partition. Consumers derive a
///     concrete filter per view to express the page query for a list of rows.
/// </summary>
public abstract record ScopedPageFilter : PageFilter
{
    protected ScopedPageFilter(AuthenticatedContext securityContext, string? query, int size,
        string? continuationToken = null, string? orderBy = null, bool descending = false, bool deleted = false)
        : base(query, size, continuationToken, orderBy, descending, deleted)
    {
        SecurityContext = securityContext;
    }

    public AuthenticatedContext SecurityContext { get; }

    /// <summary>
    ///     The identifier of the parent the rows are scoped to, used to resolve the storage partition.
    /// </summary>
    public abstract Guid? GetParentId();
}
