using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Querying;

public record PagedResultContainer(string Value) : IValueObject<string>;