using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;

namespace CascadeEsdm.WriteModel.EventStream;

public record EventDocument : IDocument
{
    public EventDocument(Guid id, string partitionKey, EventEnvelope envelope)
    {
        Id = id;
        PartitionKey = partitionKey;
        Envelope = envelope;
    }

    public EventEnvelope Envelope { get; }
    public Guid Id { get; }
    public string PartitionKey { get; }
}