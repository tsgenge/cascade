namespace CascadeEsdm.SharedKernel.Security;

public record Claim
{
    private const string DefaultIssuer = "LOCAL AUTHORITY";
    private const string DefaultValueType = "http://www.w3.org/2001/XMLSchema#string";

    public Claim(string type, string value, string? valueType = null, string? issuer = null,
        string? originalIssuer = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentNullException(nameof(type));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));

        Type = type;
        Value = value;
        ValueType = valueType ?? DefaultValueType;
        Issuer = issuer ?? DefaultIssuer;
        OriginalIssuer = originalIssuer ?? Issuer;
    }

    public string Type { get; }
    public string Value { get; }
    public string? ValueType { get; }
    public string? Issuer { get; }
    public string? OriginalIssuer { get; }

    public System.Security.Claims.Claim ToSystemClaim()
    {
        return new System.Security.Claims.Claim(Type, Value, ValueType, Issuer, OriginalIssuer);
    }

    public static Claim FromSystemClaim(System.Security.Claims.Claim claim)
    {
        if (claim is null)
            throw new ArgumentNullException(nameof(claim));

        return new Claim(claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer);
    }
}