using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     Resolves the storage <see cref="Partition" /> to query for a given filter or single-row query.
/// </summary>
public interface IQueryPartitionLocator<TView>
    where TView : IView
{
    Partition GetPartition<TFilter>(TFilter filter)
        where TFilter : ScopedPageFilter;

    Partition GetPartition(ScopedSingleQuery query);
}
