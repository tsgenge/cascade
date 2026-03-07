using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Domain.People.ValueObjects;

public record PersonId(Guid Value) : IValueObject<Guid>;