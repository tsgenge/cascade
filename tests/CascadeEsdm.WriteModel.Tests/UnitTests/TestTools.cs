using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.UnitTests;

public static class TestTools
{
    
    public static ICommandEnvelope<TestCommand> CreateCommandEnvelope(Guid? subjectId = null)
    {
        return new CommandEnvelope<TestCommand>(
            new TestCommand(subjectId ?? Guid.NewGuid()),
            new AuthenticatedContext(new(Guid.NewGuid()), new (Guid.NewGuid())),
            ClientChannel.Empty);
    }

    public static EventEnvelope CreateEventEnvelope(IDomainEvent? @event = null, int? lastSequenceNumber = null)
    {
        return new EventEnvelope(
            EventSource.ForAggregate<TestAggregate, TestCommand>(Guid.NewGuid()), 
            Subject.ForAggregate<TestAggregate>(Guid.NewGuid()),
            new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid())),
            ClientChannel.Empty,
            @event ?? Substitute.For<IDomainEvent>(),
            lastSequenceNumber ?? 1);
    }
    
    public static CommandResponse CreateCommandResponse<TCommand>(ICommandEnvelope<TCommand> envelope, IReadOnlyList<IEventEnvelope>? events = null)
        where TCommand : ICommand
    {
        var subject = envelope.Command.GetSubject(envelope);
        
        return new CommandResponse(
            envelope,
            subject,
            events ?? new List<IEventEnvelope>());
    }
}