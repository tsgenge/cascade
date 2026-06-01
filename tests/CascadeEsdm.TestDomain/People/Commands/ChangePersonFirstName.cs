using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.TestDomain.People.Events;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.WriteModel;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;

namespace CascadeEsdm.TestDomain.People.Commands;

[CommandLock(CommandLockLevel.Aggregate)]
public record ChangePersonFirstName(PersonId PersonId, FirstName FirstName, int? Timeout = null) : ICommand
{
    public Subject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<PersonAggregate>(PersonId.Value);
    }
}

internal class ChangePersonFirstNameExecutor : ICommandExecutor<ChangePersonFirstName, PersonAggregate>
{
    public async IAsyncEnumerable<EventEnvelope> ExecuteAsync(ICommandEnvelope<ChangePersonFirstName> envelope,
        PersonAggregate aggregate)
    {
        if (!aggregate.Exists)
            throw new NotFoundException("The person does not exist");

        if (envelope.Command.Timeout.HasValue)
            await Task.Delay(envelope.Command.Timeout.Value);

        yield return envelope.CreateEvent(new PersonFirstNameChanged(
            envelope.Command.PersonId.Value,
            envelope.Command.FirstName), aggregate);
    }

    public Task<ISecurityDescriptor?> GetSecurityDescriptorAsync(ICommandEnvelope<ChangePersonFirstName> envelope,
        PersonAggregate aggregate)
    {
        return Task.FromResult<ISecurityDescriptor?>(aggregate.SecurityDescriptor);
    }
}