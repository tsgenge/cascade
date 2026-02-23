using Cascade.SharedKernel.ValueObjects;

namespace Cascade.SharedKernel.Querying;

public record PageContinuationToken(string? Value) : IValueObject<string?>
{
    public static implicit operator string?(PageContinuationToken token) => token.Value;
}