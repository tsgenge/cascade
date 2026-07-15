namespace CascadeEsdm.SharedKernel.Querying;

public interface IPageQuery
{
    string? Query { get; }
    int PageSize { get; }
    string? OrderBy { get; }
    bool Descending { get; }
    bool Deleted { get; }
}