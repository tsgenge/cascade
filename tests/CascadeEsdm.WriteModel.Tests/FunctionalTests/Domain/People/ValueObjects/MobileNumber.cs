using CascadeEsdm.SharedKernel.Extensions;
using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

public record MobileNumber(string Value) : IValueObject<string>
{
    public Guid ToGuid() => Value.ToGuid();
    public static implicit operator string(MobileNumber value) => value.Value;
}