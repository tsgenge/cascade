using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

public record FirstName(string Value) : IValueObject<string>
{
    public static implicit operator string(FirstName value) => value.Value;
}