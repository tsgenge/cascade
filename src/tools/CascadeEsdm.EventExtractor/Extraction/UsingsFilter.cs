using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Extraction;

/// <summary>
/// Determines which using directives should be retained in a generated event file.
/// Events are self-contained — only usings for the shared kernel and system types are kept.
/// </summary>
public static class UsingsFilter
{
    /// <summary>
    /// Namespace prefixes that are required/allowed in the events assembly.
    /// All other usings are stripped to keep events self-contained.
    /// </summary>
    private static readonly string[] AllowedPrefixes =
    [
        "System",
        "CascadeEsdm.SharedKernel",
    ];

    public static IEnumerable<UsingDirectiveSyntax> Filter(
        IEnumerable<UsingDirectiveSyntax> usings,
        string targetEventsNamespace)
    {
        foreach (var u in usings)
        {
            var name = u.Name?.ToString() ?? string.Empty;

            if (IsAllowed(name))
                yield return u;
        }
    }

    private static bool IsAllowed(string namespaceName) =>
        AllowedPrefixes.Any(prefix =>
            namespaceName.Equals(prefix, StringComparison.Ordinal) ||
            namespaceName.StartsWith(prefix + ".", StringComparison.Ordinal));
}
