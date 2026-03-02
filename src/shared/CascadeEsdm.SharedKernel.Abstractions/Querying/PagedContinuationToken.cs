using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Querying;

public record PageContinuationToken(string? Value) : IValueObject<string?>
{
    public static implicit operator string?(PageContinuationToken token) => token.Value;
}