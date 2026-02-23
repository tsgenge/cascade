using Cascade.SharedKernel.Events;
using Cascade.SharedKernel.Infrastructure.Storage;

namespace Cascade.Commands.EventStream;

public record EventDocument : IDocument
{
    public Guid Id { get; }
    public string PartitionKey { get; }
    public EventEnvelope Envelope { get; }

    public EventDocument(Guid id, string partitionKey, EventEnvelope envelope)
    {
        Id = id;
        PartitionKey = partitionKey;
        Envelope = envelope;
    }
}