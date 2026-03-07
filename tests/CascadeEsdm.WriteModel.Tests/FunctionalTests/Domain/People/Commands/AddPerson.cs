using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Exceptions;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;

public record AddPerson(FirstName FirstName, LastName LastName, MobileNumber MobileNumber) : ICommand {
    public ISubject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<PersonAggregate>(MobileNumber.ToGuid());
    }
}

internal class AddPersonExecutor : ICommandExecutor<AddPerson, PersonAggregate>
{
    public async IAsyncEnumerable<IEventEnvelope> ExecuteAsync(ICommandEnvelope<AddPerson> envelope, PersonAggregate aggregate)
    {
        if(aggregate.Exists)
            throw new ConflictException("The person already exists");

        yield return envelope.CreateEvent<PersonAggregate>(new PersonAdded(
            envelope.Command.MobileNumber.ToGuid(),
            envelope.Command.FirstName,
            envelope.Command.LastName,
            envelope.Command.MobileNumber), aggregate);

        yield return envelope.CreateEvent(new SecurityDescriptorSet(
            aggregate.SecurityDescriptor
        ), aggregate);
        
        await Task.CompletedTask;
    }

    public Task<ISecurityDescriptor?> GetAccessControlListAsync(ICommandEnvelope<AddPerson> envelope, PersonAggregate aggregate)
    {
        return Task.FromResult<ISecurityDescriptor?>(aggregate.SecurityDescriptor);
    }
}