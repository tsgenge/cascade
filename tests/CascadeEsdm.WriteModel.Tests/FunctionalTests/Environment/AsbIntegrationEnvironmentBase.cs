using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Azure.Storage.Blobs;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.WriteModel.EventStream;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Testcontainers.Azurite;
using Testcontainers.CosmosDb;
using Testcontainers.ServiceBus;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public abstract class AsbIntegrationEnvironmentBase : IAsyncLifetime
{
    private readonly AzuriteContainer _azuriteContainer;
    private readonly CosmosDbContainer _cosmosContainer;
    private readonly ServiceBusContainer _serviceBusContainer;

    protected AsbIntegrationEnvironmentBase()
    {
        _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithInMemoryPersistence()
            .Build();

        _cosmosContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
            .Build();

        _serviceBusContainer = new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .WithConfig(GetAsbConfigPath())
            .Build();

        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
    }

    public IServiceProvider ServiceProvider { get; protected set; } = null!;
    public IFixture Fixture { get; }

    public async Task InitializeAsync()
    {
        await _azuriteContainer.StartAsync();
        await _cosmosContainer.StartAsync();
        await _serviceBusContainer.StartAsync();

        var azuriteConnectionString = _azuriteContainer.GetConnectionString();
        var cosmosConnectionString = _cosmosContainer.GetConnectionString().Replace("http:", "https:");
        var serviceBusConnectionString = _serviceBusContainer.GetConnectionString();
        Console.WriteLine($"CosmosDb connection string: {cosmosConnectionString}");

        await SetupAzurite(azuriteConnectionString);
        await SetupCosmos(cosmosConnectionString);

        var builder = new HostBuilder()
            .ConfigureAppConfiguration((context, config) => { })
            .ConfigureServices((b, services) =>
                ConfigureServices(services, azuriteConnectionString, cosmosConnectionString,
                    serviceBusConnectionString));

        var app = builder.Build();
        ServiceProvider = app.Services;
        app.Start();
    }

    public async Task DisposeAsync()
    {
        await _azuriteContainer.DisposeAsync();
        await _cosmosContainer.DisposeAsync();
        await _serviceBusContainer.DisposeAsync();
    }

    protected abstract void ConfigureServices(IServiceCollection services, string azuriteConnectionString,
        string cosmosConnectionString, string serviceBusConnectionString);

    protected static string GetAsbConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "FunctionalTests", "Environment", "service-bus-config.json");
    }

    public static void SetupEventStream(IPagedContainer<EventStreamContainer> docContainer,
        IEnumerable<EventEnvelope> events)
    {
        docContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PageResult<EventDocument>(
                events.Select(e => new EventDocument(e.Id, "fake-partition", e)).ToList(),
                new PageContinuationToken(null)));
    }

    public void SetupEventStream(IEnumerable<EventEnvelope> events)
    {
        var container = ServiceProvider.GetRequiredService<IPagedContainer<EventStreamContainer>>();
        SetupEventStream(container, events);
    }

    protected static CosmosClientOptions CreateEmulatorClientOptions()
    {
        return new CosmosClientOptions
        {
            HttpClientFactory = () =>
            {
                var innerHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(innerHandler);
            },
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true
        };
    }

    private static async Task SetupCosmos(string connectionString)
    {
        using var client = new CosmosClient(connectionString, CreateEmulatorClientOptions());

        const int maxRetries = 10;
        for (var i = 0; i < maxRetries; i++) {
            try {
                var db = await client.CreateDatabaseIfNotExistsAsync("cascade");
                await db.Database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(new EventStreamContainer().Name, "/partitionKey"));
                return;
            }
            catch (HttpRequestException) when (i < maxRetries - 1) {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            catch (Exception) { }
        }
    }

    private static async Task SetupAzurite(string connectionString)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        await blobServiceClient.CreateBlobContainerAsync("distributed-locks");
    }
}
