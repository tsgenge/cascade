using Cascade.Commands.Exceptions;
using Cascade.Commands.Hydration;
using Cascade.SharedKernel.Events;
using Cascade.SharedKernel.Infrastructure.Storage;

namespace Cascade.Commands.EventStream;

internal interface IEventStreamWriter
{
    void Add(IEventEnvelope @event);
    Task SaveAsync();
}

internal class EventStreamWriter<TContainer> : IEventStreamWriter
    where TContainer : IDocumentContainerDefinition
{
    private readonly IPartitionedContainer<TContainer> _container;
    private readonly IAggregatePartitionLocator _partitionLocator;
    private readonly HashSet<IEventEnvelope> _events = new();

    public EventStreamWriter(IPartitionedContainer<TContainer> container, IAggregatePartitionLocator partitionLocator)
    {
        _container = container;
        _partitionLocator = partitionLocator;
    }

    public void Add(IEventEnvelope @event)
    {
        _events.Add(@event);
    }

    public async Task SaveAsync()
    {
        if (_events.Count == 0)
            return;

        var partition = _partitionLocator.GetPartition(_events.First().Subject);

        var docs = _events.Select(e => new EventDocument(e.Id, partition, e)).ToList();

        try
        {
            await _container.AddBatchAsync(docs);
        }
        catch (Exception ex)
        {
            throw new EventWritingException(ex);
        }
    }
}
