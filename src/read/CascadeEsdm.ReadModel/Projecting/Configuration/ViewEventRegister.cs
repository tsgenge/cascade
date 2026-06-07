using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal interface IViewEventRegister<TView>
    where TView : IView
{
    void RegisterEvent(Type eventType, EventStateMonitor monitor);
    Type[] GetEvents();
    bool TryGetEventMonitor(Type eventType, out EventStateMonitor? monitor);
}

internal class ViewEventRegister<TView> : IViewEventRegister<TView>
    where TView : IView
{
    private readonly Dictionary<Type, EventStateMonitor> _registeredEvents = new();

    public void RegisterEvent(Type eventType, EventStateMonitor monitor)
    {
        _registeredEvents.TryAdd(eventType, monitor);
    }

    public Type[] GetEvents()
    {
        return _registeredEvents.Keys.ToArray();
    }

    public bool TryGetEventMonitor(Type eventType, out EventStateMonitor? monitor)
    {
        return _registeredEvents.TryGetValue(eventType, out monitor);
    }
}
