using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.TestDomain.People.ValueObjects;

public record PersonId(Guid Value) : IValueObject<Guid>;
