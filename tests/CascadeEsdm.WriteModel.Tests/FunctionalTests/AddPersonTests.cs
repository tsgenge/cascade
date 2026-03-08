using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class AddPersonTests : TestBase
{
    private readonly ICommandHandler<AddPerson> _sut;
    
    public AddPersonTests(ITestOutputHelper output, WriteContext environment) : base(output, environment)
    {
        //_sut = environment.ServiceProvider.GetRequiredService<ICommandHandler<AddPerson>>();
    }

    [Fact]
    public async Task AddsPerson()
    {
        await Task.CompletedTask;
    }
}