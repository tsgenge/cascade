using CascadeEsdm.WriteModel.Exceptions;

namespace CascadeEsdm.WriteModel.Policies;

internal sealed record DispatcherKey
{
    public static readonly DispatcherKey Default = new((string?)null);

    private DispatcherKey(string? value)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
            throw new ConfigurationException("A policy dispatcher key cannot be empty or whitespace.");

        Value = value;
    }

    public string? Value { get; }

    public bool IsKeyed => Value is not null;

    public static DispatcherKey For(string? name) =>
        name is null ? Default : new DispatcherKey(name);

    public static implicit operator DispatcherKey(string? name) => For(name);

    public static implicit operator string?(DispatcherKey key) => key.Value;

    public override string ToString() => Value ?? "(default)";
}
