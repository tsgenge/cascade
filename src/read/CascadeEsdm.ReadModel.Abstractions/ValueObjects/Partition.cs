using CascadeEsdm.ReadModel.Utilities;

namespace CascadeEsdm.ReadModel.ValueObjects;

/// <summary>
///     A resolved storage partition key for a view. See <see cref="Views.PartitionFormatAttribute" />.
/// </summary>
public record Partition
{
    public Partition(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>
    ///     Maps the partition to the <see cref="NotificationGroup" /> whose subscribers care about it,
    ///     deriving a deterministic group identifier from the partition key.
    /// </summary>
    public NotificationGroup AsNotificationGroup() => new(Value.ToGuid());
}
