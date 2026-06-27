namespace CascadeEsdm.SharedKernel.Querying;

public interface IContinuousPageResult<out TModel> : IPageResult<TModel>
{
    PageContinuationToken ContinuationToken { get; }
}