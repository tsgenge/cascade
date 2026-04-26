using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.Storage.CosmosDb;

public class CosmosStorageBuilder
{
    private string _databaseName = "cascade";
    private string _connectionString = string.Empty;
    private readonly InfrastructureBuilder _infraBuilder;
    
    internal CosmosStorageBuilder(InfrastructureBuilder infraBuilder)
    {
        _infraBuilder = infraBuilder ?? throw new ArgumentNullException(nameof(infraBuilder));
    }
    
    public CosmosStorageBuilder WithEventStreamContainer<TContainer>()
        where TContainer : IDocumentContainerDefinition, new()
    {
        _infraBuilder.EventStreamContainerType = typeof(TContainer);
        
        return this;
    }
    
    public CosmosStorageBuilder WithConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }
    
    public CosmosStorageBuilder WithDatabaseName(string databaseName)
    {
        _databaseName = databaseName;
        return this;
    }
    
    internal void Build()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("CosmosDB connection string is required, use WithConnectionString()");
        if (string.IsNullOrWhiteSpace(_databaseName))
            throw new InvalidOperationException("CosmosDB database name is required, use WithDatabaseName()");
        if (_infraBuilder.EventStreamContainerType == null)
            throw new InvalidOperationException("Set the Event Stream Container using WithEventStreamContainer()");

        _infraBuilder.Services.AddSingleton<CosmosOptions>(_ => new CosmosOptions
        {
            DatabaseName = _databaseName
        });

        _infraBuilder.Services.AddSingleton(_ => new CosmosClient(_connectionString));
        _infraBuilder.Services.AddGeneric(typeof(IPartitionedContainer<>), typeof(CosmosDbContainer<>));
    }
}
