using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;

namespace CascadeEsdm.DistributedLocks;

internal class DistributedLockProvider : IDistributedLockProvider
{
    private readonly Medallion.Threading.IDistributedLockProvider _innerProvider;

    public DistributedLockProvider(Medallion.Threading.IDistributedLockProvider innerProvider)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
    }

    public async Task<IDistributedLock> AcquireLockAsync(string lockName)
    {
        var @lock = _innerProvider.CreateLock(lockName);
        return new  DistributedLock(await @lock.AcquireAsync());
    }
}