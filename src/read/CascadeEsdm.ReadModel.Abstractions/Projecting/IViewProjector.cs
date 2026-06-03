using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     The entry point for the projection (write-to-read) side of a view. Given an event envelope it applies
///     the event to the affected rows of <typeparamref name="TView" /> and reports the outcome.
/// </summary>
public interface IViewProjector<TView>
    where TView : class, IView
{
    Task<ProjectionResult<TView>> ProjectAsync(EventEnvelope @event);
}
