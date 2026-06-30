using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace CascadeEsdm.SharedKernel.Security;

public record UserIdentity
{
    public UserIdentity(string value, IReadOnlyCollection<Claim>? claims = null)
    {
        if (!Guid.TryParse(value, out var id))
            throw new ArgumentOutOfRangeException("The value must be a valid Guid.");

        Id = id;
        Claims = new ReadOnlyCollection<Claim>((claims ?? Enumerable.Empty<Claim>()).ToList());
    }

    public UserIdentity(Guid id, IEnumerable<Claim>? claims = null)
    {
        Id = id;
        Claims = new ReadOnlyCollection<Claim>((claims ?? Enumerable.Empty<Claim>()).ToList());
    }

    [JsonConstructor]
    public UserIdentity(Guid id, IReadOnlyCollection<Claim> claims)
    {
        Id = id;
        Claims = new ReadOnlyCollection<Claim>((claims ?? Enumerable.Empty<Claim>()).ToList());
    }

    public IReadOnlyCollection<Claim> Claims { get; }

    public Guid Id { get; }

    public override string ToString()
    {
        return Id.ToString("n");
    }

    public Claim ToClaim()
    {
        return new Claim(ClaimTypes.Sid, ToString());
    }

    public ClaimsIdentity ToClaimsIdentity()
    {
        var systemClaims = Claims.Select(c => c.ToSystemClaim()).Append(ToClaim().ToSystemClaim());
        return new ClaimsIdentity(systemClaims, "Cascade");
    }

    public bool Exists()
    {
        return !Id.Equals(Guid.Empty);
    }

    public static implicit operator Guid(UserIdentity identity)
    {
        return identity.Id;
    }
}