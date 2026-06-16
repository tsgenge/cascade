using CascadeEsdm.EventExtractor.Generation;
using CascadeEsdm.EventExtractor.Scanning;
using FluentAssertions;

namespace CascadeEsdm.EventExtractor.Tests.Generation;

public class EventsSourceWriterTests : IDisposable
{
    private readonly string _outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public EventsSourceWriterTests() => Directory.CreateDirectory(_outputDir);

    public void Dispose() => Directory.Delete(_outputDir, recursive: true);

    private EventsSourceWriter CreateWriter() =>
        new(
            outputDir: _outputDir,
            namespaceMapper: new NamespaceMapper("Acme.WriteModel", "Acme.Events"),
            overwrite: false,
            eventToAggregateMap: new Dictionary<string, string>(),
            aggregateRoots: []);

    [Fact]
    public void WriteIsExternalInitPolyfill_WritesPolyfillFile()
    {
        var writer = CreateWriter();

        var result = writer.WriteIsExternalInitPolyfill();

        result.Should().NotBeNull();
        result!.Kind.Should().Be(WrittenFileKind.Polyfill);
        File.Exists(result.Path).Should().BeTrue();
    }

    [Fact]
    public void WriteIsExternalInitPolyfill_ContainsIsExternalInitClass()
    {
        var writer = CreateWriter();

        var result = writer.WriteIsExternalInitPolyfill();

        var content = File.ReadAllText(result!.Path);
        content.Should().Contain("System.Runtime.CompilerServices");
        content.Should().Contain("IsExternalInit");
    }

    [Fact]
    public void WriteIsExternalInitPolyfill_ReturnsNull_WhenFileUnchanged()
    {
        var writer = CreateWriter();
        writer.WriteIsExternalInitPolyfill();

        var result = writer.WriteIsExternalInitPolyfill();

        result.Should().BeNull();
    }
}
