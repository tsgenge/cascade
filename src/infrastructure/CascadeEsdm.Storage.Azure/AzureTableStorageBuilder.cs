using Azure.Data.Tables;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.Storage.Azure;

public class AzureTableStorageBuilder
{
    private readonly InfrastructureBuilder _infraBuilder;
    private string _connectionString = string.Empty;

    internal AzureTableStorageBuilder(InfrastructureBuilder infraBuilder)
    {
        _infraBuilder = infraBuilder ?? throw new ArgumentNullException(nameof(infraBuilder));
    }

    public AzureTableStorageBuilder WithConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }

    internal void Build()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException(
                "Azure Table Storage connection string is required, use WithConnectionString()");

        _infraBuilder.Services.AddSingleton(_ => new TableServiceClient(_connectionString));
        _infraBuilder.Services.AddGeneric(typeof(IEntityTableClient<>), typeof(EntityTableClient<>),
            ServiceLifetime.Singleton);
        _infraBuilder.Services.AddGeneric(typeof(ITableStore<>), typeof(AzureTableStore<>));
    }
}
