using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Querying;

public record PageContinuationToken : IValueObject<string?>
{
    public string? Value { get; }

    public PageContinuationToken(string? value)
    {
        Value = value;
    }

    public static implicit operator string?(PageContinuationToken token) => token.Value;
}