using AutoFixture;
using AutoFixture.AutoNSubstitute;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.WriteModel.EventStream;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

/// <summary>
/// Base for a single integration test class's dependency-injection configuration.
/// Containers are supplied by the shared <see cref="SharedContainerFixture"/>; this type only
/// builds and hosts the DI container, so every derived class gets isolated DI while reusing the
/// same underlying containers.
/// </summary>
public abstract class IntegrationEnvironment : IAsyncLifetime
{
    private IHost? _host;
    private bool _attached;

    protected IntegrationEnvironment()
    {
        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IFixture Fixture { get; }

    /// <summary>
    /// Builds and starts this environment's DI host using the shared containers. Idempotent, so
    /// the host is created once per test class even though it is invoked from each test's constructor.
    /// </summary>
    public void Attach(SharedContainerFixture containers)
    {
        if (_attached) {
            return;
        }

        _attached = true;

        var builder = new HostBuilder()
            .ConfigureAppConfiguration((_, _) => { })
            .ConfigureServices((_, services) =>
                ConfigureServices(services, containers.AzuriteConnectionString,
                    containers.CosmosConnectionString, containers.ServiceBusConnectionString));

        _host = builder.Build();
        ServiceProvider = _host.Services;
        _host.Start();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_host is not null) {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    protected abstract void ConfigureServices(IServiceCollection services, string azuriteConnectionString,
        string cosmosConnectionString, string serviceBusConnectionString);

    protected static CosmosClientOptions CreateEmulatorClientOptions() =>
        SharedContainerFixture.CreateEmulatorClientOptions();

    public static void SetupEventStream(IPagedContainer<EventStreamContainer> docContainer,
        IEnumerable<EventEnvelope> events)
    {
        docContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                events.Select(e => new EventDocument(e.Id, "fake-partition", e)).ToList(),
                new PageContinuationToken(null)));
    }
}
