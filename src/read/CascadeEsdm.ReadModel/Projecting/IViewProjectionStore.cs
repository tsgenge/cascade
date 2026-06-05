using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     Persistence gateway used by the projector to load, save, and delete view rows
///     during projection.
/// </summary>
internal interface IViewProjectionStore<TView>
    where TView : IView
{
    Task<(IList<TView> Rows, Partition Partition)> GetRowsAsync(EventEnvelope @event);
    Task<IReadOnlyList<Projection<TView>>> DeleteAsync(EventEnvelope @event);
    Task SaveAsync(IEnumerable<TView> rows, EventEnvelope @event);
}
