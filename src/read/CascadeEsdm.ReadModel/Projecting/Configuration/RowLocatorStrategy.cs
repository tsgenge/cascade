using AutoMapper;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.ReadModel.Querying;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal class RowLocatorStrategy<TView, TEvent> : EventBuilder<TView, TEvent>
    where TView : IView
    where TEvent : IDomainEvent
{
    public RowLocatorStrategy(IMappingExpression<TEvent, TView> expression, Profile profile, EventStateMonitor<TView, TEvent> stateMonitor) : base(expression, profile, stateMonitor) { }

    public MutationStrategy<TView, TEvent> UsingRowLocator(Func<TEvent, EventEnvelope?, KeyValuePair<string, Guid>> locatorExpression, QueryOperation? queryOperation = null)
    {
        Expression.UsingRowLocator(Profile, locatorExpression, queryOperation);
        return new MutationStrategy<TView, TEvent>(Expression, Profile, StateMonitor);
    }
}
