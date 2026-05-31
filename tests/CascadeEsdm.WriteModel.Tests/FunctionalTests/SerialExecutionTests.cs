using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using CascadeEsdm.WriteModel.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class SerialExecutionTests : TestBase
{
    private readonly ICommandHandler<AddPerson> _addPersonHandler;
    private readonly ICommandHandler<ChangePersonFirstName> _changeFirstNameHandler;
    private readonly IEventStreamReader _eventStreamReader;

    public SerialExecutionTests(ITestOutputHelper output, WriteContext environment) : base(output, environment)
    {
        _addPersonHandler = environment.ServiceProvider.GetRequiredService<ICommandHandler<AddPerson>>();
        _changeFirstNameHandler =
            environment.ServiceProvider.GetRequiredService<ICommandHandler<ChangePersonFirstName>>();
        _eventStreamReader = environment.ServiceProvider.GetRequiredService<IEventStreamReader>();
    }

    [Fact]
    public async Task Commands_ForSameAggregate_ExecuteInSequence_WhenFiredConcurrently()
    {
        var personAdded = await ExecuteAddPersonAsync();
        var personId = new PersonId(personAdded.Id);

        var firstNames = Enumerable.Range(0, 5)
            .Select(_ => new FirstName($"name-{Guid.NewGuid()}"))
            .ToList();

        var envelopes = firstNames
            .Select(firstName => new CommandEnvelope<ChangePersonFirstName>(
                new ChangePersonFirstName(personId, firstName, 50),
                GetContext(),
                ClientChannel.Empty))
            .ToList();

        var tasks = envelopes
            .Select(envelope => _changeFirstNameHandler.HandleAsync(envelope))
            .ToList();

        var events = await Task.WhenAll(tasks);

        var allEvents = await _eventStreamReader.ReadAllAsync<PersonAggregate>(personId.Value);
        var changeEvents = allEvents
            .Where(e => e.Event is PersonFirstNameChanged)
            .OrderBy(e => e.Sequence)
            .ToList();

        changeEvents.Should().HaveCount(5);

        var expectedCommandIds = envelopes.Select(e => e.Id).ToList();
        var actualCommandIds = changeEvents.Select(e => e.Source.CommandId).ToList();

        actualCommandIds.Should().Equal(expectedCommandIds);
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