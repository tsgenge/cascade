using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel.CommandHandling;

public static class CommandExtensions
{
    public static IEventEnvelope CreateEvent<TForAggregate>(this ICommandEnvelope commandEnvelope, IDomainEvent @event, TForAggregate aggregate)
        where TForAggregate : IAggregateRoot
    {
        aggregate.LastSequence = aggregate.LastSequence + 1;
        
        return new EventEnvelope(
            EventSource.ForAggregate<TForAggregate>(commandEnvelope.Id, commandEnvelope.Type),
            commandEnvelope.Command.GetSubject(commandEnvelope),
            commandEnvelope.SecurityContext,
            commandEnvelope.Channel,
            @event,
            aggregate.LastSequence);
    }
}