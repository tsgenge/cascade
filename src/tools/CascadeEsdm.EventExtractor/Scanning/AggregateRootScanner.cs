using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Scans source files to find classes implementing IAggregateRoot.
/// Uses a single pass over the already-enumerated file set to avoid redundant I/O.
/// </summary>
internal static class AggregateRootScanner
{
    /// <summary>
    /// Scans all .cs files under the source root for classes that implement IAggregateRoot.
    /// Results are cached per invocation to avoid repeated file I/O.
    /// </summary>
    public static IReadOnlyList<AggregateRootInfo> FindAggregateRoots(string sourceRoot)
    {
        var results = new List<AggregateRootInfo>();

        foreach (var filePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(filePath);

            // Quick textual pre-filter to skip files that cannot contain IAggregateRoot
            if (!source.Contains("IAggregateRoot", StringComparison.Ordinal))
                continue;

            var tree = CSharpSyntaxTree.ParseText(source, path: filePath);
            var root = tree.GetCompilationUnitRoot();

            var namespaceName = root
                .DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString() ?? string.Empty;

            var aggregateClasses = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(IsAggregateRootClass);

            foreach (var cls in aggregateClasses)
            {
                results.Add(new AggregateRootInfo(cls.Identifier.Text, namespaceName));
            }
        }

        return results;
    }

    private static bool IsAggregateRootClass(ClassDeclarationSyntax cls) =>
        cls.BaseList?.Types.Any(t =>
        {
            var typeName = t.Type.ToString();
            return typeName == "IAggregateRoot"
                || typeName.EndsWith(".IAggregateRoot", StringComparison.Ordinal);
        }) ?? false;
}
