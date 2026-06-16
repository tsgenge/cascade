namespace CascadeEsdm.EventExtractor.Generation;

public sealed record WrittenFile(string Path, WrittenFileKind Kind);

public enum WrittenFileKind
{
    EventRecord,
    Enum,
    Polyfill,
}
