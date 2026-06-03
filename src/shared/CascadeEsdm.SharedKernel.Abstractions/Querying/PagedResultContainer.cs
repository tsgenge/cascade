using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.SharedKernel.Querying;

public record PagedResultContainer : IValueObject<string>
{
    public string Value { get; }

    public PagedResultContainer(string value)
    {
        Value = value;
    }

    public static implicit operator string(PagedResultContainer container) => container.Value;
}