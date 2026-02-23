using Cascade.SharedKernel.ValueObjects;

namespace Cascade.SharedKernel.Querying;

public record PagedResultContainer(string Value) : IValueObject<string>;