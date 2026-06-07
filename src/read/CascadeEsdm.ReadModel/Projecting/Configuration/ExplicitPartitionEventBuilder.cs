using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

public class ExplicitPartitionEventBuilder<TView>
    where TView : IView
{
    private readonly ViewEventBuilder<TView> _root;

    internal ExplicitPartitionEventBuilder(ViewEventBuilder<TView> root)
    {
        _root = root;
    }

    public ExplicitPartitionStrategy<TView, TEvent> For<TEvent>() where TEvent : IDomainEvent
    {
        var monitor = new EventStateMonitor<TView, TEvent>();
        _root.EventRegistered<TEvent>(monitor);

        _root.Profile.CreateMap<SupportedEvent<TEvent>, TView>();

        return new ExplicitPartitionStrategy<TView, TEvent>(_root.Profile.CreateMap<TEvent, TView>(), _root.Profile, monitor);
    }
}
