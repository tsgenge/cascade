using Azure;
using Azure.Data.Tables;

namespace CascadeEsdm.Storage.Azure;

internal class EmbeddedEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Payload { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
