using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.Infrastructure.Serialisation;

internal static class SchemaTypeNameMapper
{
    private static readonly string[] StripSuffixes =
        [".WriteModel", ".Domain", ".Write", ".Application"];

    private static readonly Regex AssemblyComponentPattern =
        new(@",\s*([\w\.]+)$", RegexOptions.Compiled);

    public static string RewriteToSchemaTypeName(string assemblyQualifiedName)
    {
        var match = AssemblyComponentPattern.Match(assemblyQualifiedName);
        if (!match.Success)
            return assemblyQualifiedName;

        var writeModelAssembly = match.Groups[1].Value.Trim();
        var schemaAssembly = ComputeSchemaAssemblyName(writeModelAssembly);

        if (string.Equals(writeModelAssembly, schemaAssembly, StringComparison.Ordinal))
            return assemblyQualifiedName;

        var rewrittenAssembly = AssemblyComponentPattern.Replace(
            assemblyQualifiedName, $", {schemaAssembly}");

        var rewrittenNamespace = ReplaceNamespacePrefix(
            rewrittenAssembly, writeModelAssembly, schemaAssembly);

        return rewrittenNamespace;
    }

    internal static string ComputeSchemaAssemblyName(string assemblyName)
    {
        foreach (var suffix in StripSuffixes)
        {
            if (assemblyName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return assemblyName[..^suffix.Length] + ".Schema";
        }

        return assemblyName + ".Schema";
    }

    private static string ReplaceNamespacePrefix(
        string typeName, string sourcePrefix, string targetPrefix)
    {
        if (typeName.StartsWith(sourcePrefix + ".", StringComparison.Ordinal))
            return targetPrefix + typeName[sourcePrefix.Length..];

        if (typeName.StartsWith(sourcePrefix + ",", StringComparison.Ordinal))
            return targetPrefix + typeName[sourcePrefix.Length..];

        return typeName;
    }
}
