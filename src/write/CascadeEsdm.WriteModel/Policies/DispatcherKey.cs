namespace CascadeEsdm.WriteModel.Policies;

internal sealed record DispatcherKey
{
    public static readonly DispatcherKey Default = new((string?)null);

    private DispatcherKey(string? value) => Value = value;

    public string? Value { get; }

    public bool IsKeyed => Value is not null;

    public static DispatcherKey For(string? name) =>
        name is null ? Default : new DispatcherKey(name);

    public override string ToString() => Value ?? "(default)";
}
