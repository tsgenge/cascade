using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.TestDomain.People.ValueObjects;

public record LastName(string Value) : IValueObject<string>
{
    public static implicit operator string(LastName value) => value.Value;
    public static implicit operator LastName(string value) => new(value);
}
