namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Represents a discovered IAggregateRoot implementation in the source assembly.
/// </summary>
public sealed record AggregateRootInfo(string ClassName, string Namespace);
