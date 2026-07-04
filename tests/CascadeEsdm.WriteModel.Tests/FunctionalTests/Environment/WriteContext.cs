using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Azure.Storage.Blobs;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.TestDomain.People.Commands;
using CascadeEsdm.TestDomain.People.Events;
using CascadeEsdm.TestDomain.People.Policies;
using CascadeEsdm.Testing;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Decorators;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Testcontainers.Azurite;
using Testcontainers.CosmosDb;
using Testcontainers.ServiceBus;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class WriteContext : IAsyncLifetime
{
    private readonly AzuriteContainer _azuriteContainer;
    private readonly CosmosDbContainer _cosmosContainer;
    private readonly ServiceBusContainer _sbContainer;

    public WriteContext()
    {
        _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithInMemoryPersistence()
            .Build();

        _cosmosContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
            .Build();

        _sbContainer = new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .WithConfig(GetAsbConfigPath())
            .Build();

        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
    }

    public IServiceProvider ServiceProvider { get; private set; }
    public IFixture Fixture { get; }

    public async Task InitializeAsync()
    {
        await _azuriteContainer.StartAsync();
        await _cosmosContainer.StartAsync();
        await _sbContainer.StartAsync();

        var azuriteConnectionString = _azuriteContainer.GetConnectionString();
        var cosmosConnectionString = _cosmosContainer.GetConnectionString().Replace("http:", "https:");
        Console.WriteLine($"CosmosDb connection string: {cosmosConnectionString}");

        await SetupAzurite(azuriteConnectionString);
        await SetupCosmos(cosmosConnectionString);

        var builder = new HostBuilder()
            .ConfigureAppConfiguration((context, config) => { })
            .ConfigureServices((b, services) =>
            {
                services.AddCascadeEsdm(o =>
                {
                    o.WithInfrastructure(i =>
                        {
                            i.UsingCosmosDbStorage(cosmosConfig =>
                                {
                                    cosmosConfig
                                        .WithConnectionString(cosmosConnectionString)
                                        .WithOptions(CreateEmulatorClientOptions())
                                        .WithDatabaseName("cascade")
                                        .WithEventStreamContainer<EventStreamContainer>();
                                })
                                .UsingOpenTelemetry()
                                .UsingAzureDistributedLocks(lb =>
                                {
                                    lb.WithConnectionString(azuriteConnectionString);
                                })
                                .UsingAzureServiceBusReceiver(r =>
                                {
                                    r.WithConnectionString(_sbContainer.GetConnectionString());
                                    r.WithTopic("example-stream");
                                    r.WithSubscription("test-policies");
                                });
                        })
                        .WithWriteModel(b1 =>
                            b1
                                .UsingExecutors(h => h
                                    .AddCommandExecutor<AddPersonExecutor>()
                                    .AddCommandExecutor<ChangePersonFirstNameExecutor>()
                                    .AddCommandExecutor<RemovePersonExecutor>())
                                .UsingAppliers(h => h
                                    .AddEventApplier<PersonAddedApplier>()
                                    .AddEventApplier<PersonFirstNameChangedApplier>()
                                    .AddEventApplier<PersonRemovedApplier>()
                                    .AddEventApplier<SecurityDescriptorApplier>())
                                .UsingPolicies(p => p
                                    .AddPolicy<PersonEatenRemovesPersonPolicy>())
                                .AddPolicyListener()
                        );
                });
                services.AddSingleton(typeof(MessageChannel<>));
                services.AddGenericDecorator(typeof(ICommandHandler<>), typeof(MessageChannelHandler<>));
            });

        var app = builder.Build();
        ServiceProvider = app.Services;
        app.Start();
    }

    public async Task DisposeAsync()
    {
        await _azuriteContainer.DisposeAsync();
        await _cosmosContainer.DisposeAsync();
    }

    private string GetAsbConfigPath()
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

    private async Task SetupCosmos(string connectionString)
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
            catch (Exception ex) { }
        }
    }

    private static CosmosClientOptions CreateEmulatorClientOptions()
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

    private static async Task SetupAzurite(string connectionString)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        await blobServiceClient.CreateBlobContainerAsync("distributed-locks");
    }
}