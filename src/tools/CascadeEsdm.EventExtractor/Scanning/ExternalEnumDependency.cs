using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// An enum declaration found in a non-event file that is referenced by at least one event record.
/// </summary>
public sealed record ExternalEnumDependency(
    string EnumName,
    EnumDeclarationSyntax Declaration,
    string SourceNamespace,
    string SourceFilePath,
    IReadOnlyList<string> FileUsings);
