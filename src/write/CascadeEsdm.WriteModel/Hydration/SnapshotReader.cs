using CascadeEsdm.WriteModel.EventStream;

namespace CascadeEsdm.WriteModel.Hydration;

internal interface ISnapshotReader<TAggregate>
{
    Task<TAggregate?> GetLatestAsync(Guid subjectId);
    Task<TAggregate?> GetLatestAsync(Guid subjectId, int fromSequenceId);
}

internal class SnapshotReader<TAggregate> : ISnapshotReader<TAggregate>
{
    public Task<TAggregate?> GetLatestAsync(Guid subjectId)
    {
        return Task.FromResult(default(TAggregate));
    }

    public Task<TAggregate?> GetLatestAsync(Guid subjectId, int fromSequenceId)
    {
        return Task.FromResult(default(TAggregate));
    }
}