using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     A page of view rows together with the <see cref="NotificationGroup" /> a client should subscribe to in
///     order to receive live updates for the page.
/// </summary>
public record NotifyingPageResult<TItem> : PageResult<TItem>, INotifyingResult
{
    public NotifyingPageResult(IReadOnlyList<TItem> page, PageContinuationToken continuationToken,
        NotificationGroup notificationGroup)
        : base(page, continuationToken)
    {
        NotificationGroup = notificationGroup;
    }

    public NotificationGroup NotificationGroup { get; }
}