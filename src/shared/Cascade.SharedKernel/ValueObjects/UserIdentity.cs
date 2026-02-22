using System.Security.Claims;

namespace Cascade.SharedKernel.ValueObjects;

public record UserIdentity : IUserIdentity
{
    public Guid Value { get; }

    public UserIdentity(string value)
    {
        if (!Guid.TryParse(value, out var id))
            throw new ArgumentOutOfRangeException("The value must be a valid Guid.");

        Value = id;
    }

    public UserIdentity(Guid id)
    {
        Value = id;
    }

    public override string ToString()
    {
        return Value.ToString("n");
    }

    public Claim ToClaim()
    {
        return new Claim(ClaimTypes.Sid, ToString());
    }

    public bool Exists()
    {
        return !Value.Equals(Guid.Empty);
    }

    public static implicit operator Guid(UserIdentity identity)
    {
        return identity.Value;
    }
}