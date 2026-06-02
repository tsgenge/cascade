namespace CascadeEsdm.EventExtractor.Configuration;

public sealed class ExtractorOptions
{
    /// <summary>Absolute path to the root of the source project being analysed.</summary>
    public required string SourceRoot { get; init; }

    /// <summary>Absolute path to the directory where the generated events assembly will be written.</summary>
    public required string OutputDir { get; init; }

    /// <summary>RootNamespace of the source project (passed from MSBuild $(RootNamespace)).</summary>
    public required string RootNamespace { get; init; }

    /// <summary>
    /// Assembly name (and default root namespace) for the generated events project.
    /// Defaults to the source RootNamespace with a trailing write-model segment stripped, plus ".Schema".
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// When true, existing generated .cs files and the .csproj are overwritten on every run.
    /// When false (default), the .csproj is never overwritten and .cs files are only written if missing or changed.
    /// </summary>
    public bool Overwrite { get; init; } = false;

    /// <summary>Resolves the effective assembly name, computing a default from RootNamespace if not supplied.</summary>
    public string ResolvedAssemblyName =>
        AssemblyName ?? ComputeDefaultAssemblyName(RootNamespace);

    public string ResolvedEventsNamespace => ResolvedAssemblyName;

    private static readonly string[] StripSuffixes =
        [".WriteModel", ".Domain", ".Write", ".Application"];

    private static string ComputeDefaultAssemblyName(string rootNamespace)
    {
        foreach (var suffix in StripSuffixes)
        {
            if (rootNamespace.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return rootNamespace[..^suffix.Length] + ".Schema";
        }

        return rootNamespace + ".Schema";
    }
}
