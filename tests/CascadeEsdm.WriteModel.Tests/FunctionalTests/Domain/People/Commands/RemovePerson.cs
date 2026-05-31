using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;

public record RemovePerson(PersonId PersonId) : ICommand
{
    public Subject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<PersonAggregate>(PersonId.Value);
    }
}

internal class RemovePersonExecutor : ICommandExecutor<RemovePerson, PersonAggregate>
{
    public async IAsyncEnumerable<EventEnvelope> ExecuteAsync(ICommandEnvelope<RemovePerson> envelope,
        PersonAggregate aggregate)
    {
        if (!aggregate.Exists)
            throw new NotFoundException("The person does not exist");

        yield return envelope.CreateEvent<PersonAggregate>(new PersonRemoved(
            envelope.Command.PersonId.Value), aggregate);

        await Task.CompletedTask;
    }

    public Task<ISecurityDescriptor?> GetSecurityDescriptorAsync(ICommandEnvelope<RemovePerson> envelope,
        PersonAggregate aggregate)
    {
        return Task.FromResult<ISecurityDescriptor?>(aggregate.SecurityDescriptor);
    }
}