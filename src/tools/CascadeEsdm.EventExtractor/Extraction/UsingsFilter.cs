using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Extraction;

/// <summary>
/// Determines which using directives should be retained in a generated event file.
/// Write-model-specific namespaces that have no relevance to the events assembly are removed.
/// </summary>
public static class UsingsFilter
{
    /// <summary>
    /// Namespace prefixes that are exclusively write-model concerns and must be stripped
    /// from generated event files.
    /// </summary>
    private static readonly string[] WriteModelOnlyPrefixes =
    [
        "CascadeEsdm.WriteModel.Hydration",
        "CascadeEsdm.WriteModel.CommandHandling",
        "CascadeEsdm.WriteModel.Security",
        "CascadeEsdm.WriteModel.Composition",
        "CascadeEsdm.WriteModel.EventStream",
    ];

    public static IEnumerable<UsingDirectiveSyntax> Filter(
        IEnumerable<UsingDirectiveSyntax> usings,
        string targetEventsNamespace)
    {
        foreach (var u in usings)
        {
            var name = u.Name?.ToString() ?? string.Empty;

            if (IsWriteModelOnly(name))
                continue;

            yield return u;
        }
    }

    private static bool IsWriteModelOnly(string namespaceName) =>
        WriteModelOnlyPrefixes.Any(prefix =>
            namespaceName.Equals(prefix, StringComparison.Ordinal) ||
            namespaceName.StartsWith(prefix + ".", StringComparison.Ordinal));
}
