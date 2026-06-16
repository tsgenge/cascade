using System.CommandLine;
using CascadeEsdm.EventExtractor.Configuration;
using CascadeEsdm.EventExtractor.Diagnostics;
using CascadeEsdm.EventExtractor.Generation;
using CascadeEsdm.EventExtractor.Scanning;

var sourceRootOption = new Option<string>(
    name: "--source-root",
    description: "Absolute path to the root directory of the source project.")
{ IsRequired = true };

var outputDirOption = new Option<string>(
    name: "--output-dir",
    description: "Absolute path to the directory where the generated events assembly will be written.")
{ IsRequired = true };

var rootNamespaceOption = new Option<string>(
    name: "--root-namespace",
    description: "The RootNamespace of the source project (passed from MSBuild $(RootNamespace)).")
{ IsRequired = true };

var assemblyNameOption = new Option<string?>(
    name: "--assembly-name",
    description: "Override the generated assembly name. Defaults to stripping a write-model suffix from RootNamespace and appending .Events.");

var overwriteOption = new Option<bool>(
    name: "--overwrite",
    description: "When true, existing generated files are overwritten. When false (default), the .csproj is never overwritten and .cs files are only updated if changed.",
    getDefaultValue: () => false);

var rootCommand = new RootCommand("Extracts IDomainEvent records from a CascadeEsdm WriteModel project into a standalone events assembly.")
{
    sourceRootOption,
    outputDirOption,
    rootNamespaceOption,
    assemblyNameOption,
    overwriteOption,
};

rootCommand.SetHandler(
    (string sourceRoot, string outputDir, string rootNamespace, string? assemblyName, bool overwrite) =>
    {
        var options = new ExtractorOptions
        {
            SourceRoot = sourceRoot,
            OutputDir = outputDir,
            RootNamespace = rootNamespace,
            AssemblyName = assemblyName,
            Overwrite = overwrite,
        };

        Run(options);
    },
    sourceRootOption,
    outputDirOption,
    rootNamespaceOption,
    assemblyNameOption,
    overwriteOption);

return await rootCommand.InvokeAsync(args);

static void Run(ExtractorOptions options)
{
    // Single-pass scan for both event files and aggregate roots (avoids redundant file I/O)
    var scanResult = ProjectScanner.Scan(options.SourceRoot);
    var eventFiles = scanResult.EventFiles;
    var aggregateRoots = scanResult.AggregateRoots;

    if (eventFiles.Count == 0)
    {
        ExtractionReport.PrintNoEventsFound(options.SourceRoot);
        return;
    }

    // Build event → aggregate map by scanning all appliers across files
    var eventToAggregateMap = AggregateResolver.BuildEventToAggregateMap(eventFiles);

    var externalEnums = EnumDependencyScanner.FindExternalEnums(options.SourceRoot, eventFiles);

    EventsProjectGenerator.Generate(
        outputDir: options.OutputDir,
        assemblyName: options.ResolvedAssemblyName,
        rootNamespace: options.ResolvedEventsNamespace,
        overwrite: options.Overwrite);

    var namespaceMapper = new NamespaceMapper(
        sourceRootNamespace: options.RootNamespace,
        targetRootNamespace: options.ResolvedEventsNamespace);

    var writer = new EventsSourceWriter(
        outputDir: options.OutputDir,
        namespaceMapper: namespaceMapper,
        overwrite: options.Overwrite,
        eventToAggregateMap: eventToAggregateMap,
        aggregateRoots: aggregateRoots);

    var writtenEventFiles = writer.WriteEventFiles(eventFiles, options.RootNamespace);
    var writtenEnumFiles = writer.WriteExternalEnumFiles(externalEnums, options.ResolvedEventsNamespace);
    var writtenPolyfill = writer.WriteIsExternalInitPolyfill();

    var allWritten = writtenEventFiles
        .Concat(writtenEnumFiles)
        .Concat(writtenPolyfill is not null ? [writtenPolyfill] : [])
        .ToList();

    var removedFiles = writer.RemoveOrphanedFiles(allWritten);

    ExtractionReport.Print(eventFiles, externalEnums, allWritten, removedFiles, options.OutputDir);
}
