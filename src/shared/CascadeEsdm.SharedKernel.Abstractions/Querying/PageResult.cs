namespace CascadeEsdm.SharedKernel.Querying;

public record PageResult<TModel> : IContinuousPageResult<TModel>
{
    public IReadOnlyList<TModel> Page { get; }
    public PageContinuationToken ContinuationToken { get; }
    public PagedResultContainer Container { get; }
    
    public PageResult(IReadOnlyList<TModel> page, PageContinuationToken continuationToken, PagedResultContainer container)
    {
        Page = page;
        ContinuationToken = continuationToken;
        Container = container;
    }
}