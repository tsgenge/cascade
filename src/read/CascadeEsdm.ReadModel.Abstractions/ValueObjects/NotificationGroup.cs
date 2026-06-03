using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.ReadModel.ValueObjects;

/// <summary>
///     Identifies a group of subscribers that should be notified when the views within a partition change.
///     Backed by a <see cref="Guid" /> but exposes its canonical string form as the value.
/// </summary>
public record NotificationGroup : IValueObject<string>
{
    public NotificationGroup(string value)
    {
        if (!Guid.TryParse(value, out var result))
            throw new ArgumentException("The value must be a valid Guid.", nameof(value));

        Value = value;
        Id = result;
    }

    public NotificationGroup(Guid id)
    {
        Id = id;
        Value = id.ToString("n");
    }

    public Guid Id { get; }
    public string Value { get; }

    public static implicit operator string(NotificationGroup group) => group.Value;
}
