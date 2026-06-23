using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.ReadModel.Querying;

/// <summary>
///     A page of view rows together with the <see cref="NotificationGroup" /> a client should subscribe to in
///     order to receive live updates for the page.
/// </summary>
public record NotifyingPageResult<TItem> : PagedResult<TItem>
{
    public NotifyingPageResult(IReadOnlyList<TItem> page, PageContinuationToken continuationToken,
        PagedResultContainer container, NotificationGroup? notificationGroup = null)
        : base(page, continuationToken, container)
    {
        NotificationGroup = notificationGroup;
    }

    public NotificationGroup? NotificationGroup { get; }
}
