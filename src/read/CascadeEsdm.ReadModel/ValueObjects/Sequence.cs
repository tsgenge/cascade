using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.ReadModel.ValueObjects;

/// <summary>
///     The last projected sequence checkpoint for an aggregate subject within a view,
///     used to detect stale or out-of-order events.
/// </summary>
internal record Sequence
{
    public Sequence(Subject subject, DateTimeOffset utcWhen, long value)
    {
        Subject = subject;
        UtcWhen = utcWhen;
        Value = value;
    }

    public Subject Subject { get; }
    public DateTimeOffset UtcWhen { get; }
    public long Value { get; }

    public static Sequence Initial(Subject subject) => new(subject, DateTimeOffset.UtcNow, 0);
}
