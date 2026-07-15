using Azure.Storage.Blobs;
using CascadeEsdm.WriteModel.EventStream;
using Microsoft.Azure.Cosmos;
using Testcontainers.Azurite;
using Testcontainers.CosmosDb;
using Testcontainers.ServiceBus;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

/// <summary>
/// Owns the Docker test containers (Azurite, Cosmos DB, Service Bus) for the whole
/// functional test collection. A single instance is created once and shared across
/// every integration test class, so the containers only start once per test run.
/// Each test class still gets its own DI configuration via <see cref="IntegrationEnvironment"/>.
/// </summary>
public sealed class SharedContainerFixture : IAsyncLifetime
{
    private readonly AzuriteContainer _azuriteContainer;
    private readonly CosmosDbContainer _cosmosContainer;
    private readonly ServiceBusContainer _serviceBusContainer;

    public SharedContainerFixture()
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
    }

    public string AzuriteConnectionString { get; private set; } = null!;
    public string CosmosConnectionString { get; private set; } = null!;
    public string ServiceBusConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _azuriteContainer.StartAsync();
        await _cosmosContainer.StartAsync();
        await _serviceBusContainer.StartAsync();

        AzuriteConnectionString = _azuriteContainer.GetConnectionString();
        CosmosConnectionString = _cosmosContainer.GetConnectionString().Replace("http:", "https:");
        ServiceBusConnectionString = _serviceBusContainer.GetConnectionString();
        Console.WriteLine($"CosmosDb connection string: {CosmosConnectionString}");

        await SetupAzurite(AzuriteConnectionString);
        await SetupCosmos(CosmosConnectionString);
    }

    public async Task DisposeAsync()
    {
        await _azuriteContainer.DisposeAsync();
        await _cosmosContainer.DisposeAsync();
        await _serviceBusContainer.DisposeAsync();
    }

    public static CosmosClientOptions CreateEmulatorClientOptions()
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

    private static string GetAsbConfigPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "FunctionalTests", "Environment", "service-bus-config.json");
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
