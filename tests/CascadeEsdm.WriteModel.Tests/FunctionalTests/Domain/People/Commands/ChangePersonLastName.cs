using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;

public record ChangePersonLastName(PersonId PersonId, LastName LastName) : ICommand
{
    public Subject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<PersonAggregate>(PersonId.Value);
    }
}

internal class ChangePersonLastNameExecutor : ICommandExecutor<ChangePersonLastName, PersonAggregate>
{
    public async IAsyncEnumerable<EventEnvelope> ExecuteAsync(ICommandEnvelope<ChangePersonLastName> envelope,
        PersonAggregate aggregate)
    {
        if (!aggregate.Exists)
            throw new NotFoundException("The person does not exist");

        yield return envelope.CreateEvent<PersonAggregate>(new PersonLastNameChanged(
            envelope.Command.PersonId.Value,
            envelope.Command.LastName), aggregate);

        await Task.CompletedTask;
    }

    public Task<ISecurityDescriptor?> GetSecurityDescriptorAsync(ICommandEnvelope<ChangePersonLastName> envelope,
        PersonAggregate aggregate)
    {
        return Task.FromResult<ISecurityDescriptor?>(aggregate.SecurityDescriptor);
    }
}