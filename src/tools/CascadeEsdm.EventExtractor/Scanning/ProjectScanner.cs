using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Scans all .cs files under a source root and identifies files that contain
/// at least one record implementing IDomainEvent.
/// </summary>
public static class ProjectScanner
{
    public static IReadOnlyList<ScannedEventFile> FindEventFiles(string sourceRoot)
    {
        var results = new List<ScannedEventFile>();

        foreach (var filePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(source, path: filePath);
            var root = tree.GetCompilationUnitRoot();

            var eventRecords = root
                .DescendantNodes()
                .OfType<RecordDeclarationSyntax>()
                .Where(IsEventRecord)
                .ToList();

            if (eventRecords.Count == 0)
                continue;

            var namespaceName = root
                .DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString() ?? string.Empty;

            var enumDeclarations = root
                .DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .ToList();

            var applierClasses = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(IsApplierClass)
                .ToList();

            results.Add(new ScannedEventFile(
                FilePath: filePath,
                SourceNamespace: namespaceName,
                SyntaxRoot: root,
                EventRecords: eventRecords,
                EnumDeclarations: enumDeclarations,
                ApplierClasses: applierClasses));
        }

        return results;
    }

    private static bool IsEventRecord(RecordDeclarationSyntax record) =>
        record.BaseList?.Types.Any(t =>
            t.Type.ToString() is "IDomainEvent") ?? false;

    private static bool IsApplierClass(ClassDeclarationSyntax cls) =>
        cls.BaseList?.Types.Any(t =>
        {
            var typeName = t.Type.ToString();
            return typeName.StartsWith("IEventApplier<", StringComparison.Ordinal)
                || typeName == "IEventApplier";
        }) ?? false;
}
