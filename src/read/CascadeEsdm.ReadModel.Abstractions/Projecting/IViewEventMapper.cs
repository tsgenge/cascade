using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     Applies the data carried by an event to a view row and, for row-creating events,
///     resolves the identifier of the new row.
/// </summary>
public interface IViewEventMapper<TView>
    where TView : IView
{
    void Map(TView view, EventEnvelope @event);
    Guid GetNewRowId(EventEnvelope @event);
}
