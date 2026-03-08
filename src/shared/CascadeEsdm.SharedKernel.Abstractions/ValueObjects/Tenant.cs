namespace CascadeEsdm.SharedKernel.ValueObjects;

public interface ITenant : IValueObject<Guid>;

public record Tenant(Guid Value) : ITenant;