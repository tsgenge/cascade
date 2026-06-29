using System.Security.Claims;

namespace CascadeEsdm.SharedKernel.ValueObjects;

public record UserIdentity : IValueObject<Guid>
{
    public UserIdentity(string value, EmailAddress? email = null)
    {
        if (!Guid.TryParse(value, out var id))
            throw new ArgumentOutOfRangeException("The value must be a valid Guid.");

        Value = id;
        Email = email;
    }

    public UserIdentity(Guid id, EmailAddress? email = null)
    {
        Value = id;
        Email = email;
    }

    public EmailAddress? Email { get; }

    public Guid Value { get; }

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