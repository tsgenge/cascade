using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.TestDomain.People.ValueObjects;

public record FirstName(string Value) : IValueObject<string>
{
    public static implicit operator string(FirstName value) => value.Value;
    public static implicit operator FirstName(string value) => new(value);
}
