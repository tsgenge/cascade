using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Exceptions;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;

public record ChangePersonMobileNumber(PersonId PersonId, MobileNumber MobileNumber) : ICommand {
    public ISubject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<PersonAggregate>(PersonId.Value);
    }
}

internal class ChangePersonMobileNumberExecutor : ICommandExecutor<ChangePersonMobileNumber, PersonAggregate>
{
    public async IAsyncEnumerable<IEventEnvelope> ExecuteAsync(ICommandEnvelope<ChangePersonMobileNumber> envelope, PersonAggregate aggregate)
    {
        if(!aggregate.Exists)
            throw new NotFoundException("The person does not exist");

        yield return envelope.CreateEvent<PersonAggregate>(new PersonMobileNumberChanged(
            envelope.Command.PersonId.Value,
            envelope.Command.MobileNumber), aggregate);
        
        await Task.CompletedTask;
    }

    public Task<ISecurityDescriptor?> GetSecurityDescriptorAsync(ICommandEnvelope<ChangePersonMobileNumber> envelope, PersonAggregate aggregate)
    {
        return Task.FromResult<ISecurityDescriptor?>(aggregate.SecurityDescriptor);
    }
}
