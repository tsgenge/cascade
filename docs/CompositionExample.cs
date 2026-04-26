using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.Examples;

public class CompositionExample
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCascadeEsdm(cascade => cascade
            .WithInfrastructure(infra => infra
                .UseCosmosDbStorage<AppConfiguration>(storage => storage
                    .EventStreamContainer<EventStreamContainer>()
                    .WithContainer<OrderReadModelContainer>()
                    .WithContainer<CustomerReadModelContainer>())
                .UseAzureDistributedLocks<AppConfiguration>(config => config.AzureStorageConnectionString)
                .UseApplicationInsights())
            .WithWriteModel(write => write
                .RegisterWriteModel()
                .RegisterCommandsFromAssembly<OrderAggregate>()
                .RegisterCommandsFromAssembly<CustomerAggregate>()));
    }
}

public class AppConfiguration
{
    public string AzureStorageConnectionString { get; set; } = string.Empty;
    public string CosmosDbConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public class EventStreamContainer : IDocumentContainerDefinition
{
    public string Name => "eventstreams";
    public string PartitionKeyPath => "/partitionKey";
}

public class OrderReadModelContainer : IDocumentContainerDefinition
{
    public string Name => "orders";
    public string PartitionKeyPath => "/customerId";
}

public class CustomerReadModelContainer : IDocumentContainerDefinition
{
    public string Name => "customers";
    public string PartitionKeyPath => "/region";
}

public class OrderAggregate { }
public class CustomerAggregate { }
