using CascadeEsdm.EventExtractor.Extraction;
using CascadeEsdm.EventExtractor.Scanning;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Generation;

/// <summary>
/// Writes extracted event source files and any external enum dependencies
/// into the target output directory, preserving aggregate/Events folder structure.
/// </summary>
public sealed class EventsSourceWriter
{
    private readonly string _outputDir;
    private readonly NamespaceMapper _namespaceMapper;
    private readonly bool _overwrite;
    private readonly IReadOnlyDictionary<string, string> _eventToAggregateMap;

    public EventsSourceWriter(
        string outputDir,
        NamespaceMapper namespaceMapper,
        bool overwrite,
        IReadOnlyDictionary<string, string> eventToAggregateMap)
    {
        _outputDir = outputDir;
        _namespaceMapper = namespaceMapper;
        _overwrite = overwrite;
        _eventToAggregateMap = eventToAggregateMap;
    }

    public IReadOnlyList<WrittenFile> WriteEventFiles(IReadOnlyList<ScannedEventFile> eventFiles, string sourceRootNamespace)
    {
        var written = new List<WrittenFile>();

        foreach (var file in eventFiles)
        {
            var aggregateFolder = GetAggregateBasedFolder(file, sourceRootNamespace);
            var relativeFolder = aggregateFolder
                ?? _namespaceMapper.GetRelativeOutputFolder(file.SourceNamespace);

            var targetNamespace = aggregateFolder != null
                ? _namespaceMapper.FolderToNamespace(aggregateFolder)
                : _namespaceMapper.MapNamespace(file.SourceNamespace);

            var outputFolder = string.IsNullOrEmpty(relativeFolder)
                ? _outputDir
                : Path.Combine(_outputDir, relativeFolder);

            Directory.CreateDirectory(outputFolder);

            var fileName = Path.GetFileName(file.FilePath);
            var outputPath = Path.Combine(outputFolder, fileName);

            var extractedSource = EventSyntaxExtractor.Extract(file, targetNamespace, sourceRootNamespace);

            if (ShouldWrite(outputPath, extractedSource))
            {
                File.WriteAllText(outputPath, extractedSource);
                written.Add(new WrittenFile(outputPath, WrittenFileKind.EventRecord));
            }
        }

        return written;
    }

    /// <summary>
    /// Determines the folder path based on aggregate names from appliers (e.g., "Person/Events").
    /// Returns null if aggregates are unknown or mixed within the file.
    /// </summary>
    private string? GetAggregateBasedFolder(ScannedEventFile file, string sourceRootNamespace)
    {
        var aggregates = file.EventRecords
            .Select(r => AggregateResolver.GetAggregateForEvent(r, file.SourceNamespace, sourceRootNamespace, _eventToAggregateMap))
            .Where(a => !string.IsNullOrEmpty(a))
            .Distinct()
            .ToList();

        // If all events in the file belong to the same aggregate, use that
        if (aggregates.Count == 1)
        {
            var aggregateName = aggregates[0]!;
            return Path.Combine(aggregateName, "Events");
        }

        // Mixed or unknown aggregates - fall back to namespace-based
        return null;
    }

    public IReadOnlyList<WrittenFile> WriteExternalEnumFiles(
        IReadOnlyList<ExternalEnumDependency> enums,
        string targetRootNamespace)
    {
        if (enums.Count == 0)
            return [];

        var written = new List<WrittenFile>();
        var enumsFolder = Path.Combine(_outputDir, "Enums");
        Directory.CreateDirectory(enumsFolder);

        var enumsNamespace = $"{targetRootNamespace}.Enums";

        foreach (var dep in enums)
        {
            var fileName = dep.EnumName + ".cs";
            var outputPath = Path.Combine(enumsFolder, fileName);

            var source = BuildEnumFile(dep, enumsNamespace);

            if (ShouldWrite(outputPath, source))
            {
                File.WriteAllText(outputPath, source);
                written.Add(new WrittenFile(outputPath, WrittenFileKind.Enum));
            }
        }

        return written;
    }

    private bool ShouldWrite(string outputPath, string newContent)
    {
        if (!File.Exists(outputPath))
            return true;

        if (_overwrite)
            return true;

        var existing = File.ReadAllText(outputPath);
        return !string.Equals(existing, newContent, StringComparison.Ordinal);
    }

    private static string BuildEnumFile(ExternalEnumDependency dep, string targetNamespace)
    {
        var enumDecl = dep.Declaration
            .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        var ns = SyntaxFactory.FileScopedNamespaceDeclaration(
                SyntaxFactory.ParseName(targetNamespace))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(enumDecl));

        var unit = SyntaxFactory.CompilationUnit()
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(ns))
            .NormalizeWhitespace();

        return unit.ToFullString();
    }
}
