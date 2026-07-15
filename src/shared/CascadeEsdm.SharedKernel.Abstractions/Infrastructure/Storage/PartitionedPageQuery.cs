using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

public record PartitionedPageQuery : PageQuery
{
    public PartitionedPageQuery(PageQuery originalQuery, string partitionKey, Dictionary<string, string> parameters)
        : base(originalQuery.Query, originalQuery.PageSize, originalQuery.ContinuationToken, originalQuery.OrderBy,
            originalQuery.Descending, originalQuery.Deleted)
    {
        PartitionKey = partitionKey;
        QueryParameters = parameters;
    }

    public string PartitionKey { get; set; }
    public Dictionary<string, string> QueryParameters { get; set; }
}