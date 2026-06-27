using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     The query entry point for a view: serves both paged list queries and single-row look-ups.
/// </summary>
public interface IQueryHandler<TView, in TFilter, in TQuery>
    : IPageQueryHandler<TView, TFilter>, ISingleQueryHandler<TView, TQuery, Guid>
    where TView : IView
    where TFilter : IPageQuery
    where TQuery : ISingleQuery<Guid> { }

/// <summary>
///     The query entry point for a view with a custom Key type: serves both paged list queries and single-row look-ups.
/// </summary>
public interface IQueryHandler<TView, in TFilter, in TQuery, TKey>
    : IPageQueryHandler<TView, TFilter>, ISingleQueryHandler<TView, TQuery, TKey>
    where TFilter : IPageQuery
    where TQuery : ISingleQuery<TKey>
    where TKey : IEquatable<TKey> { }

/// <summary>
///     Serves a page of <typeparamref name="TView" /> rows matching <typeparamref name="TFilter" />.
/// </summary>
public interface IPageQueryHandler<TView, in TFilter>
    where TFilter : IPageQuery
{
    Task<NotifyingPageResult<TView>> GetPageAsync(TFilter filter, CancellationToken token = default);
}

/// <summary>
///     Serves a single <typeparamref name="TView" /> row matching <typeparamref name="TQuery" />.
/// </summary>
public interface ISingleQueryHandler<TView, in TQuery, TKey>
    where TQuery : ISingleQuery<TKey>
    where TKey : IEquatable<TKey>
{
    Task<NotifyingSingleResult<TView>> GetSingleAsync(TQuery query, CancellationToken token = default);
}