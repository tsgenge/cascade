namespace CascadeEsdm.SharedKernel.ValueObjects;

public record Tenant(Guid Value) : IValueObject<Guid>;