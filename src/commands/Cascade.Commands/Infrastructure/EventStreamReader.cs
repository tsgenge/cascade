using Cascade.Commands.Hydration;
using Cascade.SharedKernel.Aggregates;
using Cascade.SharedKernel.Events;
using Cascade.SharedKernel.ValueObjects;

namespace Cascade.Commands.Infrastructure;

internal interface IEventStreamReader
{
    Task<IEnumerable<IEventEnvelope>> ReadAllAsync<TAggregate>(Guid id) where TAggregate : IAggregateRoot;
    Task<EventEnvelope?> ReadSingleAsync<TAggregate>(Guid id);
}

internal class EventStreamReader<TContainer> : IEventStreamReader
    where TContainer : IDocumentContainerDefinition
{
    private readonly IPagedContainer<TContainer> _container;
    private readonly IAggregatePartitionLocator _partitionLocator;

    public EventStreamReader(IPagedContainer<TContainer> container, IAggregatePartitionLocator partitionLocator)
    {
        _container = container;
        _partitionLocator = partitionLocator;
    }

    public async Task<IEnumerable<IEventEnvelope>> ReadAllAsync<TAggregate>(Guid id)
        where TAggregate : IAggregateRoot
    {
        var partitionKey = _partitionLocator.GetPartition(new Subject(id, typeof(TAggregate).Name));

        var allEvents = new List<EventEnvelope>();
        string? continuationToken = null;
        var events = await _container.GetPageAsync<EventDocument>(new PartitionedPageQuery(new PageFilter(string.Empty, 100, continuationToken), partitionKey, new Dictionary<string, string>()));
        do {
            foreach (var doc in events.Page) {
                var evt = ConvertToEvent(doc);
                if (evt != null)
                    allEvents.Add(evt);
            }

            continuationToken = events.ContinuationToken.Value;
        } while (!string.IsNullOrWhiteSpace(continuationToken));

        return allEvents;
    }

    public async Task<EventEnvelope?> ReadSingleAsync<TAggregate>(Guid id)
    {
        var partitionKey = _partitionLocator.GetPartition(new Subject(id, typeof(TAggregate).Name));
        var any = await _container.GetPageAsync<EventDocument>(new PartitionedPageQuery(new PageFilter(string.Empty, 1), partitionKey, new Dictionary<string, string>()));
        var rawEvent = any.Page.FirstOrDefault();
        return ConvertToEvent(rawEvent);
    }

    private static EventEnvelope? ConvertToEvent(EventDocument? doc)
    {
        return doc?.Envelope;
    }
}