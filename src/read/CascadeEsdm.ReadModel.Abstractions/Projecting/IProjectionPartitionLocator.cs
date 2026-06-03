using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     Resolves the storage <see cref="Partition" /> a view row belongs to for an incoming event, when projecting.
/// </summary>
public interface IProjectionPartitionLocator<TView>
    where TView : IView
{
    Partition GetPartition(EventEnvelope @event);
}
