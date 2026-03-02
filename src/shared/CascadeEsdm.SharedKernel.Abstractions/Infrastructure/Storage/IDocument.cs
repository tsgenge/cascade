namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

public interface IDocument
{
    Guid Id { get; }
    string PartitionKey { get; }
}