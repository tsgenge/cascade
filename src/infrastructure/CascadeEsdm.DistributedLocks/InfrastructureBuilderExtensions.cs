using Azure.Storage.Blobs;
using CascadeEsdm.DistributedLocks;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;
using Medallion.Threading.Azure;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureBuilderExtensions
{
    private const string ContainerName = "distributedlocks";
    
    public static InfrastructureBuilder UseAzureDistributedLocks(
        this InfrastructureBuilder builder,
        Action<DistributedLockBuilder> configure)
    {
        var lockBuilder = new DistributedLockBuilder(builder);

        configure(lockBuilder);

        lockBuilder.Build();
        
        return builder;
    }
}
