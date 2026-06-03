using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.ValueObjects;

/// <summary>
///     The result of applying an event to a single view row: the row, the partition it lives in,
///     and the effect the event had on it.
/// </summary>
public record Projection<TView>
    where TView : IView
{
    public Projection(ProjectionEffect effect, TView view, Partition partition)
    {
        Effect = effect;
        View = view;
        Partition = partition;
    }

    public ProjectionEffect Effect { get; }
    public TView View { get; }
    public Partition Partition { get; }
}

/// <summary>
///     The effect an event had on a view row.
/// </summary>
public enum ProjectionEffect
{
    Added,
    Changed,
    Removed
}
