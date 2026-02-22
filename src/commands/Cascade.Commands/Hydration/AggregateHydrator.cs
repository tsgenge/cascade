using Cascade.Commands.Infrastructure;
using Cascade.SharedKernel.Aggregates;
using Cascade.SharedKernel.Security;

namespace Cascade.Commands.Hydration;

internal interface IAggregateHydrator<TAggregate> where TAggregate : IAggregateRoot
{
    Task<TAggregate> HydrateAsync(Guid subjectId, IAuthenticatedContext context);
}

internal class AggregateHydrator<TAggregate> : IAggregateHydrator<TAggregate> where TAggregate : IAggregateRoot
{
    private readonly IEventStreamReader _streamReader;
    private readonly IAggregateFactory _aggregateFactory;

    public AggregateHydrator(IEventStreamReader streamReader, IAggregateFactory aggregateFactory)
    {
        _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        _aggregateFactory = aggregateFactory ?? throw new ArgumentNullException(nameof(aggregateFactory));
    }

    public async Task<TAggregate> HydrateAsync(Guid subjectId, IAuthenticatedContext context)
    {
        var events = await _streamReader.ReadAllAsync<TAggregate>(subjectId);

        try
        {
            return _aggregateFactory.GetAggregator<TAggregate>(events);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to instance the aggregate for hydration ({typeof(TAggregate).Name}).", ex);
        }
    }
}