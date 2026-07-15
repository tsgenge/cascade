using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     The query entry point for a view: serves both paged list queries and single-row look-ups.
/// </summary>
public interface IQueryHandler<TView, in TFilter, in TQuery, TPageResult>
    : IPageQueryHandler<TView, TFilter, TPageResult>, ISingleQueryHandler<TView, TQuery, Guid>
    where TView : IView
    where TFilter : IPageQuery
    where TQuery : ISingleQuery<Guid>
    where TPageResult : IPageResult<TView> { }

/// <summary>
///     The query entry point for a view with a custom Key type: serves both paged list queries and single-row look-ups.
/// </summary>
public interface IQueryHandler<TView, in TFilter, in TQuery, TKey, TPageResult>
    : IPageQueryHandler<TView, TFilter, TPageResult>, ISingleQueryHandler<TView, TQuery, TKey>
    where TFilter : IPageQuery
    where TQuery : ISingleQuery<TKey>
    where TKey : IEquatable<TKey>
    where TPageResult : IPageResult<TView> { }

/// <summary>
///     Serves a page of <typeparamref name="TView" /> rows matching <typeparamref name="TFilter" />.
/// </summary>
public interface IPageQueryHandler<TView, in TFilter, TResult>
    where TFilter : IPageQuery
    where TResult : IPageResult<TView>
{
    Task<TResult> GetPageAsync(TFilter filter, CancellationToken token = default);
}

/// <summary>
///     Serves a single <typeparamref name="TView" /> row matching <typeparamref name="TQuery" />.
/// </summary>
public interface ISingleQueryHandler<TView, in TQuery, TKey>
    where TQuery : ISingleQuery<TKey>
    where TKey : IEquatable<TKey>
{
    Task<ISingleResult<TView>> GetSingleAsync(TQuery query, CancellationToken token = default);
}