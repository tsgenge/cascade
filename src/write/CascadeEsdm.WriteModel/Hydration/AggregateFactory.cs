using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CascadeEsdm.WriteModel.Hydration;

internal interface IAggregateFactory<TAggregate> where TAggregate : class, IAggregateRoot
{
    TAggregate GetAggregator(IEnumerable<IEventEnvelope> events, TAggregate? snapshot);
}

internal class AggregateFactory<TAggregate> : IAggregateFactory<TAggregate>
    where TAggregate : class, IAggregateRoot
{
    private readonly IEventApplierFactory<TAggregate> _eventApplierFactory;
    private readonly MethodInfo _factoryGetMethod;
    private readonly MethodInfo[] _aggregateApplyMethods;

    public AggregateFactory(IEventApplierFactory<TAggregate> eventApplierFactory)
    {
        _eventApplierFactory = eventApplierFactory ?? throw new ArgumentNullException(nameof(eventApplierFactory));
        
        _aggregateApplyMethods = typeof(TAggregate).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(t => t.Name == nameof(Apply)).ToArray();
        
        _factoryGetMethod = _eventApplierFactory!.GetType().GetMethod(nameof(IEventApplierFactory<TAggregate>.GetFor))!;
    }

    public TAggregate GetAggregator(IEnumerable<IEventEnvelope> events, TAggregate? snapshot)
    {
        var aggregate = snapshot ?? Activator.CreateInstance<TAggregate>();

        foreach (var @event in events)
            Apply(aggregate, @event);

        return aggregate;
    }
    
    protected void Apply(TAggregate aggregate, IEventEnvelope @event)
    {
        var eventType = @event.Event.GetType();
        var applyMethod = _aggregateApplyMethods
                .FirstOrDefault(t => t.GetParameters()[0].ParameterType == eventType);

        if (applyMethod != null)
        {
            var parameters = applyMethod.GetParameters().Length switch
            {
                1 => new object[] { @event.Event },
                2 => new object[] { @event.Event, @event },
                _ => new object[] { }
            };

            if (parameters.Length > 0)
            {
                try
                {
                    applyMethod.Invoke(this, parameters);
                }
                catch (ExceptionBase)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new EventHydrationException(ex, eventType, GetType());
                }
            }
        }
        else
        {
            var applier = GetEventApplier(@event.Event.GetType());

            try
            {
                applier!.GetType().GetMethod(nameof(IEventApplier<IDomainEvent, TAggregate>.Apply))!
                    .Invoke(applier, [ aggregate, @event.Event, @event ]);
            }
            catch (ExceptionBase)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new EventHydrationException(ex, @event.Event.GetType(), GetType());
            }
        }

        aggregate.LastSequence = @event.Sequence;
    }
    private object? GetEventApplier(Type eventType)
    {
        try
        {
            return _factoryGetMethod.MakeGenericMethod(eventType).Invoke(_eventApplierFactory, null);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException ?? ex;
        }
    }
}