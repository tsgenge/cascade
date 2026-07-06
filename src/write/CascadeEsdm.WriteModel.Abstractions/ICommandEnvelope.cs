using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel;

public interface ICommandEnvelope
{
    Guid Id { get; }
    AuthenticatedContext SecurityContext { get; }
    ClientChannel Channel { get; }
    DateTimeOffset Time { get; }
    string Type { get; }
    ICommand Command { get; }

    EventEnvelope CreateEvent<TForAggregate>(IDomainEvent @event, TForAggregate aggregate) where TForAggregate : class;

    [Obsolete("Do not use this method in ESDM environments. This is for back compatibility only.")]
    EventEnvelope CreateEvent<TForAggregate>(IDomainEvent @event, int seqId = 0) where TForAggregate : class;
}

public interface ICommandEnvelope<out TCommand> : ICommandEnvelope
    where TCommand : ICommand
{
    new TCommand Command { get; }
}