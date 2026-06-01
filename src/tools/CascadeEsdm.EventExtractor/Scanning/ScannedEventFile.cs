using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Represents a source file that contains at least one IDomainEvent record.
/// </summary>
public sealed record ScannedEventFile(
    string FilePath,
    string SourceNamespace,
    CompilationUnitSyntax SyntaxRoot,
    IReadOnlyList<RecordDeclarationSyntax> EventRecords,
    IReadOnlyList<EnumDeclarationSyntax> EnumDeclarations,
    IReadOnlyList<ClassDeclarationSyntax> ApplierClasses);
