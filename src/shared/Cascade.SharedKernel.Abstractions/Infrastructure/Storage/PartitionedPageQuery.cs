using Cascade.SharedKernel.Querying;

namespace Cascade.SharedKernel.Infrastructure.Storage;

public record PartitionedPageQuery : PageFilter
{
    public string PartitionKey { get; set; }
    public Dictionary<string, string> QueryParameters { get; set; }

    public PartitionedPageQuery(PageFilter originalQuery, string partitionKey, Dictionary<string, string> parameters)
        : base(originalQuery.Query, originalQuery.Size, originalQuery.ContinuationToken, originalQuery.OrderBy, originalQuery.Descending, originalQuery.Deleted)
    {
        PartitionKey = partitionKey;
        QueryParameters = parameters;
    }
}
