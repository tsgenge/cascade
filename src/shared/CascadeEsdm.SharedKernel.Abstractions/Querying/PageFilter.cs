namespace CascadeEsdm.SharedKernel.Querying;

public record PageFilter
{
    public virtual string? Query { get; }
    public int Size { get; }
    public string? ContinuationToken { get; }
    public string? OrderBy { get; }
    public bool Descending { get; }
    public bool Deleted { get; }

    public PageFilter(string? query, int size, string? continuationToken = null, string? orderBy = null, bool descending = false, bool deleted = false)
    {
        Query = query;
        Size = size;
        ContinuationToken = continuationToken;
        OrderBy = orderBy;
        Descending = descending;
        Deleted = deleted;
    }

    public PageFilter Copy(string? query)
    {
        return new PageFilter(query, Size, ContinuationToken, OrderBy, Descending, Deleted);
    }
}