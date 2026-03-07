using Autofac;
using Azure.Storage.Blobs;
using Medallion.Threading.Azure;

namespace CascadeEsdm.DistributedLocks;

public class DistributedLockComposerModule : Module
{
    private const string ContainerName = "distributedlocks";

    protected override void Load(ContainerBuilder builder)
    {
        //TODO: Add injection of the connection string
        //builder.Register((IOptions<ConnectionStrings> options) => new BlobContainerClient(options.Value.StorageConnection, ContainerName));
        builder.RegisterType<AzureBlobLeaseDistributedSynchronizationProvider>().AsImplementedInterfaces();
        builder.RegisterType<DistributedLockProvider>().AsImplementedInterfaces();
    }
}