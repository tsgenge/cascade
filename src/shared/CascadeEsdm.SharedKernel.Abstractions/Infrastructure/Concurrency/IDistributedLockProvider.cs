namespace CascadeEsdm.SharedKernel.Infrastructure.Concurrency;

public interface IDistributedLockProvider
{
    Task<IDistributedLock> AcquireLockAsync(string lockName);
}

public interface IDistributedLock : IDisposable, IAsyncDisposable;