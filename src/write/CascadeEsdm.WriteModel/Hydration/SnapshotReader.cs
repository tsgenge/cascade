using CascadeEsdm.WriteModel.EventStream;

namespace CascadeEsdm.WriteModel.Hydration;

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