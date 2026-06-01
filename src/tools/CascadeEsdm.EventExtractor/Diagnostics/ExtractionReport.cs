using CascadeEsdm.EventExtractor.Generation;
using CascadeEsdm.EventExtractor.Scanning;

namespace CascadeEsdm.EventExtractor.Diagnostics;

public static class ExtractionReport
{
    public static void Print(
        IReadOnlyList<ScannedEventFile> scannedFiles,
        IReadOnlyList<ExternalEnumDependency> externalEnums,
        IReadOnlyList<WrittenFile> writtenFiles,
        string outputDir)
    {
        var totalEvents = scannedFiles.Sum(f => f.EventRecords.Count);
        var eventFilesWritten = writtenFiles.Count(f => f.Kind == WrittenFileKind.EventRecord);
        var enumFilesWritten = writtenFiles.Count(f => f.Kind == WrittenFileKind.Enum);

        Console.WriteLine($"cascade-extract-events: extraction complete");
        Console.WriteLine($"  Source files with events : {scannedFiles.Count}");
        Console.WriteLine($"  Total event records      : {totalEvents}");
        Console.WriteLine($"  External enum deps found : {externalEnums.Count}");
        Console.WriteLine($"  Event files written      : {eventFilesWritten}");
        Console.WriteLine($"  Enum files written       : {enumFilesWritten}");
        Console.WriteLine($"  Output directory         : {outputDir}");
    }

    public static void PrintNoEventsFound(string sourceRoot)
    {
        Console.WriteLine($"cascade-extract-events: no IDomainEvent records found under {sourceRoot}");
    }

    public static void PrintSkipped(string reason)
    {
        Console.WriteLine($"cascade-extract-events: skipped — {reason}");
    }
}
