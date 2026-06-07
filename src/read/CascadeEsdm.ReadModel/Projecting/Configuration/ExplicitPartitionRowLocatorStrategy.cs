using AutoMapper;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.ReadModel.Querying;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal class ExplicitPartitionRowLocatorStrategy<TView, TEvent> : EventBuilder<TView, TEvent>
    where TView : IView
    where TEvent : IDomainEvent
{
    public ExplicitPartitionRowLocatorStrategy(IMappingExpression<TEvent, TView> expression, Profile profile, EventStateMonitor<TView, TEvent> stateMonitor) : base(expression, profile, stateMonitor) { }

    public MutationStrategy<TView, TEvent> AndRowLocator(Func<TEvent, EventEnvelope?, KeyValuePair<string, Guid>> locatorExpression, QueryOperation? queryOperation = null)
    {
        StateMonitor.RowLocationMethod = RowLocationMethod.Explicit;
        Expression.UsingRowLocator(Profile, locatorExpression, queryOperation);
        return new MutationStrategy<TView, TEvent>(Expression, Profile, StateMonitor);
    }
}
