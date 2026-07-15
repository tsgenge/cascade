namespace CascadeEsdm.SharedKernel.Querying;

public interface IIndexedPageQuery : IPageQuery
{
    int Page { get; }
}