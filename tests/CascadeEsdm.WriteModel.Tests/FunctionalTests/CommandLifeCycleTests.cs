using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.TestDomain.People;
using CascadeEsdm.TestDomain.People.Commands;
using CascadeEsdm.TestDomain.People.Events;
using CascadeEsdm.TestDomain.People.ValueObjects;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class CommandLifeCycleTests : TestBase
{
    private readonly ICommandHandler<AddPerson> _addPersonHandler;
    private readonly ICommandHandler<ChangePersonFirstName> _changeFirstNameHandler;
    private readonly IPagedContainer<EventStreamContainer> _eventStreamContainer;

    public CommandLifeCycleTests(ITestOutputHelper output, WriteContext environment) : base(output, environment)
    {
        _addPersonHandler = environment.ServiceProvider.GetRequiredService<ICommandHandler<AddPerson>>();
        _changeFirstNameHandler =
            environment.ServiceProvider.GetRequiredService<ICommandHandler<ChangePersonFirstName>>();
        _eventStreamContainer = environment.ServiceProvider.GetRequiredService<IPagedContainer<EventStreamContainer>>();
    }

    [Fact]
    public async Task Adds_Person()
    {
        var command = new AddPerson(PersonDataGenerator.FirstName(), PersonDataGenerator.LastName(),
            PersonDataGenerator.MobileNumber());
        var envelope = new CommandEnvelope<AddPerson>(command, GetContext(), ClientChannel.Empty);
        var response = await _addPersonHandler.HandleAsync(envelope);

        response.Events.Should().HaveCount(2);
        response.Events[0].Event.Should()
            .BeEquivalentTo(new PersonAdded(command.MobileNumber.ToGuid(), command.FirstName.Value,
                command.LastName.Value, command.MobileNumber.Value));
    }

    [Fact]
    public async Task FollowOn_Command_LocatesAndExecutes()
    {
        var personAdded = await ExecuteAddPersonAsync();
        var newFirstName = PersonDataGenerator.FirstName();

        var command = new ChangePersonFirstName(new PersonId(personAdded.Id), newFirstName);
        var envelope = new CommandEnvelope<ChangePersonFirstName>(command, GetContext(), ClientChannel.Empty);
        var response = await _changeFirstNameHandler.HandleAsync(envelope);

        response.Events.Should().HaveCount(1);
        response.Events[0].Event.Should()
            .BeEquivalentTo(new PersonFirstNameChanged(personAdded.Id, newFirstName.Value));
    }

    [Fact]
    public async Task Change_Command_ThrowsNotFound_IfAggregateDoesNotExist()
    {
        var command = new ChangePersonFirstName(new PersonId(Guid.NewGuid()), PersonDataGenerator.FirstName());
        var envelope = new CommandEnvelope<ChangePersonFirstName>(command, GetContext(), ClientChannel.Empty);

        var act = () => _changeFirstNameHandler.HandleAsync(envelope);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<PersonAdded> ExecuteAddPersonAsync()
    {
        var command = new AddPerson(PersonDataGenerator.FirstName(), PersonDataGenerator.LastName(),
            PersonDataGenerator.MobileNumber());
        var envelope = new CommandEnvelope<AddPerson>(command, GetContext(), ClientChannel.Empty);
        var response = await _addPersonHandler.HandleAsync(envelope);
        return response.Events[0].Event.As<PersonAdded>();
    }

    private AuthenticatedContext GetContext()
    {
        return new AuthenticatedContext(
            new UserIdentity(Guid.NewGuid()),
            new Tenant(Guid.NewGuid()));
    }
}