using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Scans all .cs files under a source root and identifies files that contain
/// at least one record implementing IDomainEvent, as well as IAggregateRoot implementations.
/// </summary>
public static class ProjectScanner
{
    /// <summary>
    /// Performs a single-pass scan of all .cs files to find both event files and aggregate root classes.
    /// This avoids redundant file-system enumeration and parsing.
    /// </summary>
    public static ScanResult Scan(string sourceRoot)
    {
        var eventFiles = new List<ScannedEventFile>();
        var aggregateRoots = new List<AggregateRootInfo>();

        foreach (var filePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(filePath);
            var hasEvents = source.Contains("IDomainEvent", StringComparison.Ordinal);
            var hasAggregateRoot = source.Contains("IAggregateRoot", StringComparison.Ordinal);

            if (!hasEvents && !hasAggregateRoot)
                continue;

            var tree = CSharpSyntaxTree.ParseText(source, path: filePath);
            var root = tree.GetCompilationUnitRoot();

            var namespaceName = root
                .DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString() ?? string.Empty;

            if (hasEvents)
            {
                var eventRecords = root
                    .DescendantNodes()
                    .OfType<RecordDeclarationSyntax>()
                    .Where(IsEventRecord)
                    .ToList();

                if (eventRecords.Count > 0)
                {
                    var enumDeclarations = root
                        .DescendantNodes()
                        .OfType<EnumDeclarationSyntax>()
                        .ToList();

                    var applierClasses = root
                        .DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .Where(IsApplierClass)
                        .ToList();

                    eventFiles.Add(new ScannedEventFile(
                        FilePath: filePath,
                        SourceNamespace: namespaceName,
                        SyntaxRoot: root,
                        EventRecords: eventRecords,
                        EnumDeclarations: enumDeclarations,
                        ApplierClasses: applierClasses));
                }
            }

            if (hasAggregateRoot)
            {
                var aggregateClasses = root
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(IsAggregateRootClass);

                foreach (var cls in aggregateClasses)
                {
                    aggregateRoots.Add(new AggregateRootInfo(cls.Identifier.Text, namespaceName));
                }
            }
        }

        return new ScanResult(eventFiles, aggregateRoots);
    }

    public static IReadOnlyList<ScannedEventFile> FindEventFiles(string sourceRoot)
    {
        return Scan(sourceRoot).EventFiles;
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

    private static bool IsAggregateRootClass(ClassDeclarationSyntax cls) =>
        cls.BaseList?.Types.Any(t =>
        {
            var typeName = t.Type.ToString();
            return typeName == "IAggregateRoot"
                || typeName.EndsWith(".IAggregateRoot", StringComparison.Ordinal);
        }) ?? false;
}

/// <summary>
/// Result of a combined project scan containing both event files and aggregate root information.
/// </summary>
public sealed record ScanResult(
    IReadOnlyList<ScannedEventFile> EventFiles,
    IReadOnlyList<AggregateRootInfo> AggregateRoots);
