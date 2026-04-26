using Azure.Storage.Blobs;
using CascadeEsdm.DistributedLocks;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;
using Medallion.Threading.Azure;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureBuilderExtensions
{
    private const string ContainerName = "distributedlocks";
    
    public static InfrastructureBuilder UseAzureDistributedLocks(
        this InfrastructureBuilder builder,
        Action<DistributedLockBuilder> configure)
    {
        var lockBuilder = new DistributedLockBuilder();

        configure(lockBuilder);

        lockBuilder.Validate();

        builder.Services.AddSingleton(serviceProvider =>
        {
            return new BlobContainerClient(lockBuilder.ConnectionString, ContainerName);
        });
        
        builder.Services
            .AddTransient<Medallion.Threading.IDistributedLockProvider,
                AzureBlobLeaseDistributedSynchronizationProvider>();
        builder.Services.AddTransient<IDistributedLockProvider, DistributedLockProvider>();
        
        return builder;
    }
}
