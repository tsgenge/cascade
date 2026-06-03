using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     The query entry point for a view: serves both paged list queries and single-row look-ups.
/// </summary>
public interface IQueryHandler<TView, in TFilter, in TQuery>
    : IPageQueryHandler<TView, TFilter>, ISingleQueryHandler<TView, TQuery>
    where TView : IView
    where TFilter : ScopedPageFilter
    where TQuery : ScopedSingleQuery
{
}

/// <summary>
///     Serves a page of <typeparamref name="TView" /> rows matching <typeparamref name="TFilter" />.
/// </summary>
public interface IPageQueryHandler<TView, in TFilter>
    where TView : IView
    where TFilter : ScopedPageFilter
{
    Task<NotifyingPageResult<TView>> GetPageAsync(TFilter filter);
}

/// <summary>
///     Serves a single <typeparamref name="TView" /> row matching <typeparamref name="TQuery" />.
/// </summary>
public interface ISingleQueryHandler<TView, in TQuery>
    where TView : IView
    where TQuery : ScopedSingleQuery
{
    Task<NotifyingSingleResult<TView>> GetSingleAsync(TQuery query);
}
