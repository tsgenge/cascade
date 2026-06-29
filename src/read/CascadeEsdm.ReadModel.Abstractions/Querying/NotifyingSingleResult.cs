using CascadeEsdm.ReadModel.ValueObjects;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     A single view row together with the <see cref="NotificationGroup" /> a client should subscribe to in
///     order to receive live updates for the row.
/// </summary>
public record NotifyingSingleResult<TView> : INotifyingResult, ISingleResult<TView>
{
    public NotifyingSingleResult(TView result, NotificationGroup notificationGroup)
    {
        Result = result;
        NotificationGroup = notificationGroup;
    }

    public NotificationGroup NotificationGroup { get; }

    public TView Result { get; }
}