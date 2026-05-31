using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests;

public class EventReaderScratch : TestBase
{
    public EventReaderScratch(ITestOutputHelper output, WriteContext environment) : base(output, environment) { }

    [Fact]
    public async Task ReadsFromCosmos()
    {
        var sut = Environment.ServiceProvider.GetRequiredService<IEventStreamReader>();
        var events = await sut.ReadAllAsync<PersonAggregate>(Guid.Parse("2c2459c42cd51e4ab86592b09975b107"));
    }
}