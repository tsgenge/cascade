using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

public record LastName(string Value) : IValueObject<string>
{
    public static implicit operator string(LastName value) => value.Value;
}