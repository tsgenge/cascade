using Medallion.Threading;

namespace CascadeEsdm.DistributedLocks;

internal class DistributedLock(IDistributedSynchronizationHandle handle) : CascadeEsdm.SharedKernel.Infrastructure.Concurrency.IDistributedLock
{
    public void Dispose()
    {
        handle.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await handle.DisposeAsync();
    }
}