namespace CascadeEsdm.SharedKernel.Querying;

public interface IPageResult<out TModel>
{
    IReadOnlyList<TModel> Page { get; }
    PagedResultContainer Container { get; }
}