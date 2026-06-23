using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     The query entry point for a view: serves both paged list queries and single-row look-ups.
/// </summary>
public interface IQueryHandler<TView, in TFilter, in TQuery>
    : IPageQueryHandler<TView, TFilter>, ISingleQueryHandler<TView, TQuery, Guid>
    where TView : IView
    where TFilter : ScopedPageFilter
    where TQuery : ScopedSingleQuery<Guid> { }

/// <summary>
///     The query entry point for a view with a custom Key type: serves both paged list queries and single-row look-ups.
/// </summary>
public interface IQueryHandler<TView, in TFilter, in TQuery, TKey>
    : IPageQueryHandler<TView, TFilter>, ISingleQueryHandler<TView, TQuery, TKey>
    where TView : IView
    where TFilter : ScopedPageFilter
    where TQuery : ScopedSingleQuery<TKey>
    where TKey : IEquatable<TKey> { }

/// <summary>
///     Serves a page of <typeparamref name="TView" /> rows matching <typeparamref name="TFilter" />.
/// </summary>
public interface IPageQueryHandler<TView, in TFilter>
    where TView : IView
    where TFilter : ScopedPageFilter
{
    Task<NotifyingPageResult<TView>> GetPageAsync(TFilter filter, CancellationToken token = default);
}

/// <summary>
///     Serves a single <typeparamref name="TView" /> row matching <typeparamref name="TQuery" />.
/// </summary>
public interface ISingleQueryHandler<TView, in TQuery, TKey>
    where TView : IView
    where TQuery : ScopedSingleQuery<TKey>
    where TKey : IEquatable<TKey>
{
    Task<NotifyingSingleResult<TView>> GetSingleAsync(TQuery query, CancellationToken token = default);
}