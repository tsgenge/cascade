using AutoMapper;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

public class MutationStrategy<TView, TEvent> : EventBuilder<TView, TEvent>
    where TView : IView
    where TEvent : IDomainEvent
{
    internal MutationStrategy(IMappingExpression<TEvent, TView> expression, Profile profile, EventStateMonitor<TView, TEvent> stateMonitor) : base(expression, profile, stateMonitor) { }

    public IMappingExpression<TEvent, TView> AddsNewRow(Func<TEvent, EventEnvelope?, Guid> newIdResolver)
    {
        StateMonitor.EventRowAction = EventRowAction.Adds;
        Expression.AddsNewRow(Profile, newIdResolver);
        ConfigureBaseMappings(EventRowAction.Adds);
        return Expression;
    }

    public IMappingExpression<TEvent, TView> ChangesRows()
    {
        StateMonitor.EventRowAction = EventRowAction.Changes;
        ConfigureBaseMappings(EventRowAction.Changes);
        return Expression;
    }

    public void RemovesRows()
    {
        StateMonitor.EventRowAction = EventRowAction.Removes;
        Expression.RemovesRow(Profile);
    }

    private void ConfigureBaseMappings(EventRowAction action)
    {
        Expression.ForProperty(e => e.Modified, (e, o) => o?.Time ?? DateTimeOffset.UtcNow);
        if (action == EventRowAction.Adds)
            Expression.ForProperty(e => e.Created, (e, o) => o?.Time ?? DateTimeOffset.UtcNow);
    }
}
