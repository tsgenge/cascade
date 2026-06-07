using AutoMapper;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal class ExplicitPartitionStrategy<TView, TEvent> : EventBuilder<TView, TEvent>
    where TView : IView
    where TEvent : IDomainEvent
{
    public ExplicitPartitionStrategy(IMappingExpression<TEvent, TView> expression, Profile profile,
        EventStateMonitor<TView, TEvent> stateMonitor) : base(expression, profile, stateMonitor) { }

    public ExplicitPartitionRowLocatorStrategy<TView, TEvent> UsingPartitionIdentifier(
        Func<TEvent, EventEnvelope?, Guid> expression)
    {
        Expression.UsingPartitionKey(Profile, expression);
        return new ExplicitPartitionRowLocatorStrategy<TView, TEvent>(Expression, Profile, StateMonitor);
    }

    public ExplicitPartitionRowLocatorStrategy<TView, TEvent> UsingPartitionLocator<TTypeConverter>()
        where TTypeConverter : ITypeConverter<TEvent, ExplicitPartitionKey<TView>>
    {
        Profile.CreateMap<TEvent, ExplicitPartitionKey<TView>>().ConvertUsing<TTypeConverter>();
        return new ExplicitPartitionRowLocatorStrategy<TView, TEvent>(Expression, Profile, StateMonitor);
    }
}
