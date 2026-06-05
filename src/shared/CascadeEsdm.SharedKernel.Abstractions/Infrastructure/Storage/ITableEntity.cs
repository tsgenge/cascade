namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

public interface ITableEntity
{
    string PartitionKey { get; set; }
    string RowKey { get; set; }
    DateTimeOffset? Timestamp { get; set; }
    string ETag { get; set; }
}
