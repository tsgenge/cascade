using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;
using System.Collections.Concurrent;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal class EventCapabilityEvaluator<TView> : IEventCapabilityEvaluator<TView>
    where TView : IView
{
    private readonly IViewEventRegister<TView> _eventRegister;
    private readonly ConcurrentDictionary<string, bool> _supportCache = new();

    public EventCapabilityEvaluator(IViewEventRegister<TView> eventRegister)
    {
        _eventRegister = eventRegister ?? throw new ArgumentNullException(nameof(eventRegister));
    }

    public bool AddsRow(EventEnvelope @event)
    {
        if (_eventRegister.TryGetEventMonitor(@event.Event.GetType(), out var monitor))
            return monitor!.EventRowAction == EventRowAction.Adds;
        return false;
    }

    public bool RemovesRow(EventEnvelope @event)
    {
        if (_eventRegister.TryGetEventMonitor(@event.Event.GetType(), out var monitor))
            return monitor!.EventRowAction == EventRowAction.Removes;
        return false;
    }

    public bool Supports(EventEnvelope @event)
    {
        if (!_supportCache.TryGetValue(@event.Event.GetType().Name, out var result)) {
            result = _eventRegister.GetEvents().Any(t => t == @event.Event.GetType());
            _supportCache.TryAdd(@event.Event.GetType().Name, result);
        }

        return result;
    }
}
