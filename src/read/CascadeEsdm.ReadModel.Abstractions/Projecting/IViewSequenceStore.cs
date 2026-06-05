using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     Tracks the last projected sequence per aggregate subject for a view, enabling
///     stale-event detection and replay-gap identification.
/// </summary>
public interface IViewSequenceStore<TView>
    where TView : IView
{
    Task<Sequence> GetLastSequenceAsync(Subject subject);
    Task SaveAsync(Sequence sequence);
}
