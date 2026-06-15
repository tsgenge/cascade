using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.ReadModel.Infrastructure;

public interface IViewNotificationService
{
    Task ViewChangedAsync<TView>(IEnumerable<Projection<TView>> projections, EventEnvelope @event)
        where TView : IView;
}