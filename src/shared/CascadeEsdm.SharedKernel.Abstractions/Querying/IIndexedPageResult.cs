namespace CascadeEsdm.SharedKernel.Querying;

public interface IIndexedPageResult
{
    int TotalCount { get; }
    int PageCount { get; }
    int PageIndex { get; }
    int PageSize { get; }
}