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
    public void WriteIsExternalInitPolyfill_AlwaysReturnsFile_EvenWhenUnchanged()
    {
        var writer = CreateWriter();
        writer.WriteIsExternalInitPolyfill();

        var result = writer.WriteIsExternalInitPolyfill();

        result.Should().NotBeNull();
        result.Kind.Should().Be(WrittenFileKind.Polyfill);
    }

    [Fact]
    public void RemoveOrphanedFiles_DeletesEventFile_NotInWrittenList()
    {
        var writer = CreateWriter();

        // Simulate a previously extracted event file that no longer exists in source
        var orphanedFile = Path.Combine(_outputDir, "Person", "Events", "PersonRenamed.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(orphanedFile)!);
        File.WriteAllText(orphanedFile, "// old event");

        // The written list does not include the orphaned file
        var writtenFiles = new List<WrittenFile>();

        var removed = writer.RemoveOrphanedFiles(writtenFiles);

        File.Exists(orphanedFile).Should().BeFalse();
        removed.Should().ContainSingle()
            .Which.Should().Be(orphanedFile);
    }

    [Fact]
    public void RemoveOrphanedFiles_DoesNotDelete_FilesInWrittenList()
    {
        var writer = CreateWriter();

        // Create a file that IS in the written list (still exists in source)
        var keptFile = Path.Combine(_outputDir, "Person", "Events", "PersonCreated.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(keptFile)!);
        File.WriteAllText(keptFile, "// current event");

        var writtenFiles = new List<WrittenFile>
        {
            new(keptFile, WrittenFileKind.EventRecord)
        };

        var removed = writer.RemoveOrphanedFiles(writtenFiles);

        File.Exists(keptFile).Should().BeTrue();
        removed.Should().BeEmpty();
    }

    [Fact]
    public void RemoveOrphanedFiles_DoesNotDelete_CsprojFiles()
    {
        var writer = CreateWriter();

        var csprojFile = Path.Combine(_outputDir, "Acme.Events.csproj");
        File.WriteAllText(csprojFile, "<Project />");

        var removed = writer.RemoveOrphanedFiles(new List<WrittenFile>());

        File.Exists(csprojFile).Should().BeTrue();
        removed.Should().BeEmpty();
    }

    [Fact]
    public void RemoveOrphanedFiles_DeletesOrphanedEnumFile()
    {
        var writer = CreateWriter();

        // Simulate an orphaned enum file
        var orphanedEnum = Path.Combine(_outputDir, "Enums", "OldStatus.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(orphanedEnum)!);
        File.WriteAllText(orphanedEnum, "// old enum");

        var removed = writer.RemoveOrphanedFiles(new List<WrittenFile>());

        File.Exists(orphanedEnum).Should().BeFalse();
        removed.Should().ContainSingle()
            .Which.Should().Be(orphanedEnum);
    }

    [Fact]
    public void RemoveOrphanedFiles_RemovesEmptyDirectories_AfterDeletion()
    {
        var writer = CreateWriter();

        // Create a nested orphan so the parent dir becomes empty after deletion
        var orphanDir = Path.Combine(_outputDir, "Removed", "Events");
        Directory.CreateDirectory(orphanDir);
        File.WriteAllText(Path.Combine(orphanDir, "Gone.cs"), "// gone");

        writer.RemoveOrphanedFiles(new List<WrittenFile>());

        Directory.Exists(orphanDir).Should().BeFalse();
        Directory.Exists(Path.Combine(_outputDir, "Removed")).Should().BeFalse();
    }

    [Fact]
    public void RemoveOrphanedFiles_DoesNotDeletePolyfill_WhenInWrittenList()
    {
        var writer = CreateWriter();

        // Write polyfill first (simulates previous run), then use its path in keep list
        var polyfill = writer.WriteIsExternalInitPolyfill();

        var removed = writer.RemoveOrphanedFiles(new List<WrittenFile> { polyfill });

        File.Exists(polyfill.Path).Should().BeTrue();
        removed.Should().BeEmpty();
    }
}
