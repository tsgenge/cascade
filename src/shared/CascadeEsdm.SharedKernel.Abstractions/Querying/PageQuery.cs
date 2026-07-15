namespace CascadeEsdm.SharedKernel.Querying;

public record PageQuery : IContinuousPageQuery
{
    public PageQuery(string? query, int size, string? continuationToken = null, string? orderBy = null,
        bool descending = false, bool deleted = false)
    {
        Query = query;
        PageSize = size;
        ContinuationToken = continuationToken;
        OrderBy = orderBy;
        Descending = descending;
        Deleted = deleted;
    }

    public virtual string? Query { get; }
    public int PageSize { get; }
    public string? ContinuationToken { get; }
    public string? OrderBy { get; }
    public bool Descending { get; }
    public bool Deleted { get; }

    public PageQuery Copy(string? query)
    {
        return new PageQuery(query, PageSize, ContinuationToken, OrderBy, Descending, Deleted);
    }
}