using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Exceptions;
using CascadeEsdm.WriteModel.Exceptions;
using System.Reflection;

namespace CascadeEsdm.WriteModel.Hydration;

internal interface IAggregateFactory<TAggregate> where TAggregate : class, IAggregateRoot
{
    TAggregate GetAggregator(IEnumerable<EventEnvelope> events, TAggregate? snapshot);
}

internal class AggregateFactory<TAggregate> : IAggregateFactory<TAggregate>
    where TAggregate : class, IAggregateRoot
{
    private readonly MethodInfo[] _aggregateApplyMethods;
    private readonly IEventApplierFactory<TAggregate> _eventApplierFactory;
    private readonly MethodInfo _factoryGetMethod;

    public AggregateFactory(IEventApplierFactory<TAggregate> eventApplierFactory)
    {
        _eventApplierFactory = eventApplierFactory ?? throw new ArgumentNullException(nameof(eventApplierFactory));

        _aggregateApplyMethods = typeof(TAggregate).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(t => t.Name == nameof(Apply)).ToArray();

        _factoryGetMethod = _eventApplierFactory!.GetType().GetMethod(nameof(IEventApplierFactory<TAggregate>.GetFor))!;
    }

    public TAggregate GetAggregator(IEnumerable<EventEnvelope> events, TAggregate? snapshot)
    {
        var aggregate = snapshot ?? Activator.CreateInstance<TAggregate>();

        foreach (var @event in events)
            Apply(aggregate, @event);

        return aggregate;
    }

    protected void Apply(TAggregate aggregate, EventEnvelope @event)
    {
        var eventType = @event.Event.GetType();
        var applyMethod = _aggregateApplyMethods
            .FirstOrDefault(t => t.GetParameters()[0].ParameterType == eventType);

        if (applyMethod != null) {
            var parameters = applyMethod.GetParameters().Length switch
            {
                1 => new object[] { @event.Event },
                2 => new object[] { @event.Event, @event },
                _ => new object[] { }
            };

            if (parameters.Length > 0) {
                try {
                    applyMethod.Invoke(aggregate, parameters);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ExceptionBase) {
                    throw ex.InnerException;
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null) {
                    throw new EventHydrationException(ex.InnerException, eventType, aggregate.GetType());
                }
                catch (Exception ex) {
                    throw new EventHydrationException(ex, eventType, aggregate.GetType());
                }
            }
        }
        else {
            var applier = GetEventApplier(@event.Event.GetType());

            try {
                applier!.GetType().GetMethod(nameof(IEventApplier<IDomainEvent, TAggregate>.Apply))!
                    .Invoke(applier, [aggregate, @event.Event, @event]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ExceptionBase) {
                throw ex.InnerException;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null) {
                throw new EventHydrationException(ex.InnerException, eventType, aggregate.GetType());
            }
            catch (Exception ex) {
                throw new EventHydrationException(ex, eventType, aggregate.GetType());
            }
        }

        aggregate.LastSequence = @event.Sequence;
    }

    private object? GetEventApplier(Type eventType)
    {
        try {
            return _factoryGetMethod.MakeGenericMethod(eventType).Invoke(_eventApplierFactory, null);
        }
        catch (TargetInvocationException ex) {
            throw new EventHydrationException(ex.InnerException ?? ex, eventType, typeof(TAggregate));
        }
    }
}