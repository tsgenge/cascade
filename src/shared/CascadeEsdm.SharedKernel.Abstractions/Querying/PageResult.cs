namespace CascadeEsdm.SharedKernel.Querying;

public record PageResult<TModel> : IContinuousPageResult<TModel>
{
    public PageResult(IReadOnlyList<TModel> page, PageContinuationToken continuationToken)
    {
        Page = page;
        ContinuationToken = continuationToken;
    }

    public IReadOnlyList<TModel> Page { get; }
    public PageContinuationToken ContinuationToken { get; }
}