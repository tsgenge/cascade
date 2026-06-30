using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.ReadModel.Views;

/// <summary>
///     A view that records the identity of the user who authored (created) the row.
/// </summary>
public interface IAuthoredView : IView
{
    UserIdentity Author { get; set; }
}
