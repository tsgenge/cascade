using CascadeEsdm.EventExtractor.Scanning;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Extraction;

/// <summary>
/// Transforms a scanned event file into a new compilation unit suitable for the events assembly:
/// - Removes all IEventApplier class declarations.
/// - Rewrites the namespace to the target events namespace.
/// - Filters write-model-only using directives.
/// - Retains event records and any enums co-located in the same file.
/// </summary>
public static class EventSyntaxExtractor
{
    public static string Extract(
        ScannedEventFile file,
        string targetNamespace,
        string sourceRootNamespace)
    {
        var root = file.SyntaxRoot;

        var filteredUsings = UsingsFilter
            .Filter(root.Usings, targetNamespace)
            .ToList();

        var members = root.Members
            .SelectMany(ExpandNamespaceMembers)
            .Where(m => !IsApplierClass(m))
            .ToList();

        var namespaceSyntax = BuildNamespace(targetNamespace, members);

        var newRoot = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(filteredUsings))
            .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(namespaceSyntax))
            .NormalizeWhitespace();

        return newRoot.ToFullString();
    }

    private static IEnumerable<MemberDeclarationSyntax> ExpandNamespaceMembers(MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseNamespaceDeclarationSyntax ns => ns.Members,
            _ => [member]
        };
    }

    private static bool IsApplierClass(MemberDeclarationSyntax member)
    {
        if (member is not ClassDeclarationSyntax cls)
            return false;

        return cls.BaseList?.Types.Any(t =>
        {
            var typeName = t.Type.ToString();
            return typeName.StartsWith("IEventApplier<", StringComparison.Ordinal)
                || typeName == "IEventApplier";
        }) ?? false;
    }

    private static FileScopedNamespaceDeclarationSyntax BuildNamespace(
        string namespaceName,
        IEnumerable<MemberDeclarationSyntax> members)
    {
        var memberList = SyntaxFactory.List(
            members.Select(m => m
                .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)));

        return SyntaxFactory.FileScopedNamespaceDeclaration(
                SyntaxFactory.ParseName(namespaceName))
            .WithMembers(memberList);
    }
}
