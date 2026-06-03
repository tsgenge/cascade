namespace CascadeEsdm.SharedKernel.ValueObjects;

public record Tenant : IValueObject<Guid>
{
    public Guid Value { get; }

    public Tenant(Guid value)
    {
        Value = value;
    }

    public static implicit operator Guid(Tenant tenant) => tenant.Value;
}