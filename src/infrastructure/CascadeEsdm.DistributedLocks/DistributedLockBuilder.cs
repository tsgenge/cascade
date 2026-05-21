using Azure.Storage.Blobs;
using CascadeEsdm.DistributedLocks;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;
using Medallion.Threading.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.DistributedLocks;

public class DistributedLockBuilder
{
    private const string DefaultContainerName = "distributed-locks";
    private string _connectionString = string.Empty;
    private string? _containerName;
    private readonly InfrastructureBuilder _infraBuilder;

    public DistributedLockBuilder(InfrastructureBuilder infraBuilder)
    {
        _infraBuilder = infraBuilder ?? throw new ArgumentNullException(nameof(infraBuilder));
    }

    public DistributedLockBuilder WithConnectionString(string connectionString)
    {
        _connectionString = connectionString;
        return this;
    }

    public DistributedLockBuilder WithBlobContainer(string containerName)
    {
        _containerName = containerName;
        return this;
    }

    internal void Build()
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Connection string is required for distributed locks.");
        
        _infraBuilder.Services.AddSingleton(sp => new BlobContainerClient(_connectionString, _containerName ?? DefaultContainerName));
        
        _infraBuilder.Services
            .AddTransient<Medallion.Threading.IDistributedLockProvider,
                AzureBlobLeaseDistributedSynchronizationProvider>();
        
        _infraBuilder.Services.AddTransient<IDistributedLockProvider, DistributedLockProvider>();
    }
}
