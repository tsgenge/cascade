namespace Cascade.SharedKernel.Querying;

public record PagedResult<TModel>
{
    public IReadOnlyList<TModel> Page { get; }
    public PageContinuationToken ContinuationToken { get; }
    public PagedResultContainer Container { get; }
    
    public PagedResult(IReadOnlyList<TModel> page, PageContinuationToken continuationToken, PagedResultContainer container)
    {
        Page = page;
        ContinuationToken = continuationToken;
        Container = container;
    }
}