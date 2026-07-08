using CascadeEsdm.EventExtractor.Configuration;
using CascadeEsdm.EventExtractor.Generation;
using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Integration;

public class OtherTestDomainExtractionTests : IDisposable
{
    private readonly string _outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly string _sourceRoot = Path.GetFullPath(Path.Combine(
        typeof(OtherTestDomainExtractionTests).Assembly.Location,
        "..",
        "..",
        "..",
        "..",
        "..",
        "CascadeEsdm.OtherTestDomain"));

    private const string SourceRootNamespace = "CascadeEsdm.OtherTestDomain";

    public OtherTestDomainExtractionTests() => Directory.CreateDirectory(_outputDir);

    public void Dispose() => Directory.Delete(_outputDir, recursive: true);

    [Fact]
    public void Extract_PersonEaten_PreservesFullNamespacePath()
    {
        var options = new ExtractorOptions
        {
            SourceRoot = _sourceRoot,
            OutputDir = _outputDir,
            RootNamespace = SourceRootNamespace,
            Overwrite = true,
        };

        var scanResult = ProjectScanner.Scan(options.SourceRoot);
        var eventFiles = scanResult.EventFiles;
        var aggregateMap = AggregateResolver.BuildEventToAggregateMap(eventFiles);
        var namespaceMapper = new NamespaceMapper(options.RootNamespace, options.ResolvedEventsNamespace);
        var writer = new EventsSourceWriter(
            options.OutputDir,
            namespaceMapper,
            options.Overwrite,
            aggregateMap,
            scanResult.AggregateRoots);

        var written = writer.WriteEventFiles(eventFiles, options.RootNamespace);

        var expectedFile = Path.GetFullPath(
            Path.Combine(options.OutputDir, "Domain", "Monsters", "Events", "PersonEaten.cs"));
        written.Should().ContainSingle(w => w.Path == expectedFile);
        File.Exists(expectedFile).Should().BeTrue();

        var content = File.ReadAllText(expectedFile);
        content.Should().Contain("namespace CascadeEsdm.OtherTestDomain.Schema.Domain.Monsters.Events;");
    }

    [Fact]
    public void Extract_PersonEaten_DoesNotCollapseNamespaceToFirstSegment()
    {
        var options = new ExtractorOptions
        {
            SourceRoot = _sourceRoot,
            OutputDir = _outputDir,
            RootNamespace = SourceRootNamespace,
            Overwrite = true,
        };

        var scanResult = ProjectScanner.Scan(options.SourceRoot);
        var eventFiles = scanResult.EventFiles;
        var aggregateMap = AggregateResolver.BuildEventToAggregateMap(eventFiles);
        var namespaceMapper = new NamespaceMapper(options.RootNamespace, options.ResolvedEventsNamespace);
        var writer = new EventsSourceWriter(
            options.OutputDir,
            namespaceMapper,
            options.Overwrite,
            aggregateMap,
            scanResult.AggregateRoots);

        writer.WriteEventFiles(eventFiles, options.RootNamespace);

        var collapsedFile = Path.Combine(options.OutputDir, "Domain", "Events", "PersonEaten.cs");
        File.Exists(collapsedFile).Should().BeFalse(
            "The old fallback collapsed the namespace to Domain/Events, dropping the Monsters segment.");
    }
}
