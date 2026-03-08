using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.WriteModel.EventStream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class WriteContext
{
    public IServiceProvider ServiceProvider { get; }
    public IFixture Fixture { get; }

    public WriteContext()
    {
        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
        
        var builder = new HostBuilder()
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>((h, b) => { })
            .ConfigureAppConfiguration((context, config) => { })
            .ConfigureServices((b, services) => { });
        
        var app= builder.Build();
        ServiceProvider = app.Services;
    }
    
    public void SetupEventStream(IPagedContainer<EventStreamContainer> docContainer, IEnumerable<EventEnvelope> events)
    {
        docContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PagedResult<EventDocument>(events.Select(e => new EventDocument(e.Id, "fake-partition", e)).ToList(), new PageContinuationToken(null), new PagedResultContainer("fake-partition")));
    }

    public void SetupEventStream(IEnumerable<EventEnvelope> events)
    {
        var container = ServiceProvider.GetRequiredService<IPagedContainer<EventStreamContainer>>();
        SetupEventStream(container, events);
    }    
}