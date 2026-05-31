using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Azure.Storage.Blobs;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.Events;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Testcontainers.Azurite;
using Testcontainers.CosmosDb;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class WriteContext : IAsyncLifetime
{
    private readonly AzuriteContainer _azuriteContainer;
    private readonly CosmosDbContainer _cosmosContainer;

    public WriteContext()
    {
        _azuriteContainer = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithInMemoryPersistence()
            .Build();

        _cosmosContainer = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")
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
                            i.UseCosmosDbStorage(cosmosConfig =>
                                {
                                    cosmosConfig
                                        .WithConnectionString(cosmosConnectionString)
                                        .WithOptions(CreateEmulatorClientOptions())
                                        .WithDatabaseName("cascade")
                                        .WithEventStreamContainer<EventStreamContainer>();
                                })
                                .UseApplicationInsights()
                                .UseAzureDistributedLocks(lb =>
                                {
                                    lb.WithConnectionString(azuriteConnectionString);
                                });
                        })
                        .WithWriteModel(b1 =>
                            b1
                                .WithExecutors(h => h
                                    .AddCommandExecutor<AddPerson, AddPersonExecutor, PersonAggregate>()
                                    .AddCommandExecutor<ChangePersonFirstName, ChangePersonFirstNameExecutor,
                                        PersonAggregate>())
                                .WithAppliers(h =>
                                {
                                    h.RegisterEventApplier<PersonAdded, PersonAddedApplier, PersonAggregate>();
                                    h.RegisterEventApplier<PersonFirstNameChanged, PersonFirstNameChangedApplier,
                                        PersonAggregate>();
                                    h.RegisterEventApplier<PersonLastNameChanged, PersonLastNameChangedApplier,
                                        PersonAggregate>();
                                    h.RegisterEventApplier<PersonMobileNumberChanged, PersonMobileNumberChangedApplier,
                                        PersonAggregate>();
                                    h.RegisterEventApplier<PersonRemoved, PersonRemovedApplier, PersonAggregate>();
                                    h.RegisterEventApplier<SecurityDescriptorSet, SecurityDescriptorApplier,
                                        PersonAggregate>();
                                })
                        );
                });
            });

        var app = builder.Build();
        ServiceProvider = app.Services;
    }

    public async Task DisposeAsync()
    {
        await _azuriteContainer.DisposeAsync();
        await _cosmosContainer.DisposeAsync();
    }

    public static void SetupEventStream(IPagedContainer<EventStreamContainer> docContainer,
        IEnumerable<EventEnvelope> events)
    {
        docContainer.GetPageAsync<EventDocument>(Arg.Any<PartitionedPageQuery>())
            .Returns(new PagedResult<EventDocument>(
                events.Select(e => new EventDocument(e.Id, "fake-partition", e)).ToList(),
                new PageContinuationToken(null), new PagedResultContainer("fake-partition")));
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