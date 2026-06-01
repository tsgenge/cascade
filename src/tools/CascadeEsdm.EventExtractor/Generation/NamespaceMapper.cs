namespace CascadeEsdm.EventExtractor.Generation;

/// <summary>
/// Maps source namespaces and file paths to their target equivalents in the events assembly.
/// </summary>
public sealed class NamespaceMapper
{
    private readonly string _sourceRootNamespace;
    private readonly string _targetRootNamespace;

    public NamespaceMapper(string sourceRootNamespace, string targetRootNamespace)
    {
        _sourceRootNamespace = sourceRootNamespace;
        _targetRootNamespace = targetRootNamespace;
    }

    /// <summary>
    /// Maps a source namespace to its target namespace in the events assembly.
    ///
    /// Example:
    ///   source:  Acme.Orders.WriteModel.Orders.Events
    ///   target:  Acme.Orders.Events.Orders.Events
    ///
    /// The source root namespace prefix is replaced with the target root namespace.
    /// </summary>
    public string MapNamespace(string sourceNamespace)
    {
        if (sourceNamespace.StartsWith(_sourceRootNamespace, StringComparison.Ordinal))
        {
            var remainder = sourceNamespace[_sourceRootNamespace.Length..].TrimStart('.');
            return string.IsNullOrEmpty(remainder)
                ? _targetRootNamespace
                : $"{_targetRootNamespace}.{remainder}";
        }

        return $"{_targetRootNamespace}.{sourceNamespace}";
    }

    /// <summary>
    /// Derives the relative output path for a source file, preserving aggregate folder structure.
    ///
    /// Example:
    ///   source namespace: Acme.Orders.WriteModel.Orders.Events
    ///   source root ns:   Acme.Orders.WriteModel
    ///   → relative path:  Orders/Events/
    /// </summary>
    public string GetRelativeOutputFolder(string sourceNamespace)
    {
        if (sourceNamespace.StartsWith(_sourceRootNamespace, StringComparison.Ordinal))
        {
            var remainder = sourceNamespace[_sourceRootNamespace.Length..].TrimStart('.');
            if (!string.IsNullOrEmpty(remainder))
                return remainder.Replace('.', Path.DirectorySeparatorChar);
        }

        return string.Empty;
    }
}
