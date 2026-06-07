using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

public class StaticPartitionEventBuilder<TView>
    where TView : IView
{
    private readonly ViewEventBuilder<TView> _root;

    internal StaticPartitionEventBuilder(ViewEventBuilder<TView> root)
    {
        _root = root;
    }

    public RowLocatorStrategy<TView, TEvent> For<TEvent>() where TEvent : IDomainEvent
    {
        var monitor = new EventStateMonitor<TView, TEvent>();
        _root.EventRegistered<TEvent>(monitor);

        _root.Profile.CreateMap<SupportedEvent<TEvent>, TView>();

        return new RowLocatorStrategy<TView, TEvent>(_root.Profile.CreateMap<TEvent, TView>(), _root.Profile, monitor);
    }
}
