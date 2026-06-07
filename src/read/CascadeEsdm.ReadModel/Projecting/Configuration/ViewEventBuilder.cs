using AutoMapper;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

public class ViewEventBuilder<TView> : ViewEventBuilder
    where TView : IView
{
    private readonly IViewEventRegister<TView> _eventRegister;
    private PartitionResolutionMethod? _partitionMethod;

    internal Profile Profile { get; }

    internal ViewEventBuilder(Profile profile, IViewEventRegister<TView> eventRegister)
    {
        Profile = profile;
        _eventRegister = eventRegister;
    }

    public StaticPartitionEventBuilder<TView> UsesStaticPartitionKey()
    {
        _partitionMethod = PartitionResolutionMethod.Static;
        return new StaticPartitionEventBuilder<TView>(this);
    }

    public ExplicitPartitionEventBuilder<TView> UsesExplicitPartitionKey()
    {
        _partitionMethod = PartitionResolutionMethod.Dynamic;
        return new ExplicitPartitionEventBuilder<TView>(this);
    }

    internal void EventRegistered<TEvent>(EventStateMonitor monitor)
    {
        _eventRegister.RegisterEvent(typeof(TEvent), monitor);
    }

    internal override void Validate()
    {
        if (_partitionMethod == null)
            throw new ProjectionConfigurationException<TView>($"PartitionStrategy not specified with {nameof(UsesStaticPartitionKey)} or {nameof(UsesExplicitPartitionKey)}");

        if (_eventRegister.GetEvents().Length == 0)
            throw new ProjectionConfigurationException<TView>("No events registered for projection");

        foreach (var evt in _eventRegister.GetEvents()) {
            if (_eventRegister.TryGetEventMonitor(evt, out var monitor))
                monitor!.Validate();
        }
    }
}

public abstract class ViewEventBuilder
{
    internal abstract void Validate();
}

public abstract class EventBuilder<TView, TEvent>
    where TView : IView
    where TEvent : IDomainEvent
{
    protected readonly IMappingExpression<TEvent, TView> Expression;
    protected readonly Profile Profile;
    internal readonly EventStateMonitor<TView, TEvent> StateMonitor;

    internal EventBuilder(IMappingExpression<TEvent, TView> expression, Profile profile, EventStateMonitor<TView, TEvent> stateMonitor)
    {
        Expression = expression;
        Profile = profile;
        StateMonitor = stateMonitor;
    }
}

internal class EventStateMonitor<TView, TEvent> : EventStateMonitor
    where TView : IView
    where TEvent : IDomainEvent
{
    public override void Validate()
    {
        if (RowLocationMethod == null)
            throw new ProjectionConfigurationException<TView, TEvent>($"RowLocationMethod not specified using {nameof(RowLocatorStrategy<TView, TEvent>.UsingRowLocator)}");

        if (EventRowAction == null)
            throw new ProjectionConfigurationException<TView, TEvent>("EventRowAction not specified using UpdatesRows, CreatesRow or DeletesRow.");
    }
}

internal abstract class EventStateMonitor
{
    public RowLocationMethod? RowLocationMethod { get; set; }
    public EventRowAction? EventRowAction { get; set; }
    public abstract void Validate();
}

internal enum EventRowAction
{
    Changes,
    Adds,
    Removes
}

internal enum RowLocationMethod
{
    Explicit,
    Implicit
}

internal enum PartitionResolutionMethod
{
    Static,
    Dynamic
}
