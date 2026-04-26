using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using System.Runtime.Intrinsics.X86;

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
            .ConfigureAppConfiguration((context, config) => { })
            .ConfigureServices((b, services) =>
            {
                services.AddCascadeEsdm(o =>
                {
                    o.WithInfrastructure(i =>
                    {
                        i.UseCosmosDbStorage(cosmosConfig =>
                        {
                            cosmosConfig.WithConnectionString("UseDevelopmentStorage=true");
                        })
                        .UseApplicationInsights()
                        .UseAzureDistributedLocks(lb =>
                        {
                            lb.WithConnectionString("UseDevelopmentStorage=true");
                        });
                    })
                    .WithWriteModel(b1 => 
                        b1
                            .WithExecutors(
                                h => h.AddCommandExecutor<AddPerson, PersonAggregate, AddPersonExecutor>())
                            .WithAppliers(h => h.RegisterEventApplier<PersonAdded, PersonAggregate, PersonAddedApplier>())
                        );
                });
                    
            });
        
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