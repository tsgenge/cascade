using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     A single view row together with the <see cref="NotificationGroup" /> a client should subscribe to in
///     order to receive live updates for the row.
/// </summary>
public record NotifyingSingleResult<TView>
    where TView : IView
{
    public NotifyingSingleResult(TView result, NotificationGroup notificationGroup)
    {
        Result = result;
        NotificationGroup = notificationGroup;
    }

    public TView Result { get; }
    public NotificationGroup NotificationGroup { get; }
}
