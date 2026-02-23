using Cascade.Commands.EventStream;
using Cascade.SharedKernel.Aggregates;
using Cascade.SharedKernel.Security;

namespace Cascade.Commands.Hydration;

internal interface IAggregateHydrator<TAggregate> where TAggregate : IAggregateRoot
{
    Task<TAggregate> HydrateAsync(Guid subjectId, IAuthenticatedContext context);
    Task<TAggregate> HydrateAsync(Guid subjectId, int fromSequenceId, IAuthenticatedContext context);
}

internal class AggregateHydrator<TAggregate> : IAggregateHydrator<TAggregate> where TAggregate : class, IAggregateRoot
{
    private readonly IEventStreamReader _streamReader;
    private readonly IAggregateFactory<TAggregate> _aggregateFactory;
    private readonly ISnapshotReader<TAggregate> _snapshotReader;

    public AggregateHydrator(IEventStreamReader streamReader, IAggregateFactory<TAggregate> aggregateFactory,
        ISnapshotReader<TAggregate> snapshotReader)
    {
        _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        _aggregateFactory = aggregateFactory ?? throw new ArgumentNullException(nameof(aggregateFactory));
        _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
    }

    public async Task<TAggregate> HydrateAsync(Guid subjectId, IAuthenticatedContext context)
    {
        var snapshot = await _snapshotReader.GetLatestAsync(subjectId);
        var events = await _streamReader.ReadAllAsync<TAggregate>(subjectId);

        try
        {
            return _aggregateFactory.GetAggregator(events, snapshot);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to instance the aggregate for hydration ({typeof(TAggregate).Name}).", ex);
        }
    }

    public async Task<TAggregate> HydrateAsync(Guid subjectId, int fromSequenceId, IAuthenticatedContext context)
    {
        var snapshot = await _snapshotReader.GetLatestAsync(subjectId, fromSequenceId);
        var events = await _streamReader.ReadAllAsync<TAggregate>(subjectId);

        try
        {
            return _aggregateFactory.GetAggregator(events, snapshot);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to instance the aggregate for hydration ({typeof(TAggregate).Name}).", ex);
        }
    }
}