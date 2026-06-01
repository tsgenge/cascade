using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Resolves enum declarations referenced by event records but defined in files
/// that do not themselves contain any IDomainEvent records.
/// </summary>
public static class EnumDependencyScanner
{
    /// <summary>
    /// Scans all .cs files in the source root and returns enum declarations whose names
    /// are referenced by any of the supplied event records, but which were not already
    /// captured in the files returned by <see cref="ProjectScanner"/>.
    /// </summary>
    public static IReadOnlyList<ExternalEnumDependency> FindExternalEnums(
        string sourceRoot,
        IReadOnlyList<ScannedEventFile> eventFiles)
    {
        var alreadyCapturedEnumNames = eventFiles
            .SelectMany(f => f.EnumDeclarations)
            .Select(e => e.Identifier.Text)
            .ToHashSet(StringComparer.Ordinal);

        var referencedTypeNames = CollectReferencedTypeNames(eventFiles);

        var missingEnumNames = referencedTypeNames
            .Except(alreadyCapturedEnumNames, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (missingEnumNames.Count == 0)
            return [];

        var results = new List<ExternalEnumDependency>();
        var eventFilePaths = eventFiles.Select(f => f.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var filePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (eventFilePaths.Contains(filePath))
                continue;

            var source = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(source, path: filePath);
            var root = tree.GetCompilationUnitRoot();

            var matchingEnums = root
                .DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .Where(e => missingEnumNames.Contains(e.Identifier.Text))
                .ToList();

            if (matchingEnums.Count == 0)
                continue;

            var namespaceName = root
                .DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.ToString() ?? string.Empty;

            var usings = root.Usings.Select(u => u.ToString()).ToList();

            foreach (var enumDecl in matchingEnums)
            {
                results.Add(new ExternalEnumDependency(
                    EnumName: enumDecl.Identifier.Text,
                    Declaration: enumDecl,
                    SourceNamespace: namespaceName,
                    SourceFilePath: filePath,
                    FileUsings: usings));

                missingEnumNames.Remove(enumDecl.Identifier.Text);
            }

            if (missingEnumNames.Count == 0)
                break;
        }

        return results;
    }

    private static HashSet<string> CollectReferencedTypeNames(IReadOnlyList<ScannedEventFile> eventFiles)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in eventFiles)
        {
            foreach (var record in file.EventRecords)
            {
                foreach (var param in record.ParameterList?.Parameters ?? [])
                {
                    CollectTypeNames(param.Type, names);
                }
            }
        }

        return names;
    }

    private static void CollectTypeNames(TypeSyntax? type, HashSet<string> names)
    {
        if (type is null) return;

        switch (type)
        {
            case IdentifierNameSyntax id:
                names.Add(id.Identifier.Text);
                break;
            case NullableTypeSyntax nullable:
                CollectTypeNames(nullable.ElementType, names);
                break;
            case GenericNameSyntax generic:
                foreach (var arg in generic.TypeArgumentList.Arguments)
                    CollectTypeNames(arg, names);
                break;
            case QualifiedNameSyntax qualified:
                names.Add(qualified.Right.Identifier.Text);
                break;
        }
    }
}
