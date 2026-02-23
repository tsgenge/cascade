namespace Cascade.Commands.EventStream;

internal interface ISnapshotReader<TAggregate>
{
    Task<TAggregate?> GetLatestAsync(Guid subjectId);
    Task<TAggregate?> GetLatestAsync(Guid subjectId, int fromSequenceId);
}