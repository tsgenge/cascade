using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class AddPersonTests : TestBase
{
    private readonly IPagedContainer<EventStreamContainer> _eventStreamContainer;
    private readonly ICommandHandler<AddPerson> _sut;

    public AddPersonTests(ITestOutputHelper output, WriteContext environment) : base(output, environment)
    {
        _sut = environment.ServiceProvider.GetRequiredService<ICommandHandler<AddPerson>>();
        _eventStreamContainer = environment.ServiceProvider.GetRequiredService<IPagedContainer<EventStreamContainer>>();
    }

    [Fact]
    public async Task AddsPerson()
    {
        var command = new AddPerson(new FirstName("Tim"), new LastName("Genge"), new MobileNumber("07545778041"));
        var envelope = new CommandEnvelope<AddPerson>(
            command,
            GetContext(),
            ClientChannel.Empty
        );
        var response = await _sut.HandleAsync(envelope);

        response.Events.Should().HaveCount(2);
        response.Events[0].Event.Should()
            .BeEquivalentTo(new PersonAdded(command.MobileNumber.ToGuid(), command.FirstName.Value,
                command.LastName.Value, command.MobileNumber.Value));
        
        PersonAdded personAdded = response.Events[0].Event.As<PersonAdded>();

        var changeFirstName = new ChangePersonFirstName(new PersonId(personAdded.Id), new FirstName("NewFirstName"));
        var envelope2 = new CommandEnvelope<ChangePersonFirstName>(
            changeFirstName,
            GetContext(),
            ClientChannel.Empty);
        
        var sut2 = Environment.ServiceProvider.GetRequiredService<ICommandHandler<ChangePersonFirstName>>();
        var response2 = await sut2.HandleAsync(envelope2);
        
        response2.Events.Should().HaveCount(1);
        response2.Events[0].Event.Should().BeEquivalentTo(new PersonFirstNameChanged(changeFirstName.PersonId.Value, changeFirstName.FirstName.Value));
    }

    private AuthenticatedContext GetContext()
    {
        return new AuthenticatedContext(
            new UserIdentity(Guid.NewGuid()),
            new Tenant(Guid.NewGuid()));
    }
}