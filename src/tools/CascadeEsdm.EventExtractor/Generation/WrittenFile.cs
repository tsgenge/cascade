namespace CascadeEsdm.EventExtractor.Generation;

public sealed record WrittenFile(string Path, WrittenFileKind Kind, bool WasModified);

public enum WrittenFileKind
{
    EventRecord,
    Enum,
    Polyfill,
}
