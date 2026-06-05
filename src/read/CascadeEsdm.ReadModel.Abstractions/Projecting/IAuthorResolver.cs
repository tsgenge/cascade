using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     Resolves the <see cref="UserIdentity" /> of the user who authored a view row,
///     used when projecting into an <see cref="Views.IAuthoredView" />.
/// </summary>
public interface IAuthorResolver
{
    Task<UserIdentity?> ResolveAsync(AuthenticatedContext context);
}
