using Cascade.SharedKernel.Events;
using Cascade.SharedKernel.Infrastructure.Storage;

namespace Cascade.Commands.EventStream;

public record EventDocument : IDocument
{
    public Guid Id { get; }
    public string PartitionKey { get; }
    public IEventEnvelope Envelope { get; }

    public EventDocument(Guid id, string partitionKey, IEventEnvelope envelope)
    {
        Id = id;
        PartitionKey = partitionKey;
        Envelope = envelope;
    }
}