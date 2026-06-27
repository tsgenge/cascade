namespace CascadeEsdm.SharedKernel.Querying;

public interface IContinuousPageQuery : IPageQuery
{
    string? ContinuationToken { get; }
}