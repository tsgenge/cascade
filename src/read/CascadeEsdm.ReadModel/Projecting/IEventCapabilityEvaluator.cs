using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     Determines whether a view supports a given event and what structural effect
///     (add / change / remove row) the event has on the view.
/// </summary>
internal interface IEventCapabilityEvaluator<TView>
    where TView : IView
{
    bool Supports(EventEnvelope @event);
    bool AddsRow(EventEnvelope @event);
    bool RemovesRow(EventEnvelope @event);
}
