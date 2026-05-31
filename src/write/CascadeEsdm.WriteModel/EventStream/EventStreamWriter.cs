using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;

namespace CascadeEsdm.WriteModel.EventStream;

internal interface IEventStreamWriter
{
    void Add(EventEnvelope @event);
    Task SaveAsync();
}

internal class EventStreamWriter<TContainer> : IEventStreamWriter
    where TContainer : IDocumentContainerDefinition
{
    private readonly IPartitionedContainer<TContainer> _container;
    private readonly HashSet<EventEnvelope> _events = new();
    private readonly IAggregatePartitionLocator _partitionLocator;

    public EventStreamWriter(IPartitionedContainer<TContainer> container, IAggregatePartitionLocator partitionLocator)
    {
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _partitionLocator = partitionLocator ?? throw new ArgumentNullException(nameof(partitionLocator));
    }

    public void Add(EventEnvelope @event)
    {
        _events.Add(@event);
    }

    public async Task SaveAsync()
    {
        if (_events.Count == 0)
            return;

        var partition = _partitionLocator.GetPartition(_events.First().Subject);

        var docs = _events.Select(e => new EventDocument(e.Id, partition, e)).ToList();

        try {
            await _container.AddBatchAsync(docs);
        }
        catch (Exception ex) {
            throw new EventWritingException(ex);
        }
        finally {
            _events.Clear();
        }
    }
}