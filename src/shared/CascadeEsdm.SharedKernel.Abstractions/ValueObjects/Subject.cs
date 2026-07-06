using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Extensions;
using CascadeEsdm.SharedKernel.Validation;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.ValueObjects;

public record Subject : IValueObject<string>
{
    private const string Pattern =
        $@"([\w]+)(\/({ValidationPatterns.GuidPattern}))?\/({ValidationPatterns.GuidPattern})";

    public Subject(string value)
    {
        Parse(value, out var id, out var parentId, out var type);
        Id = id;
        Type = type;
        Parent = parentId;
        Value = FormatValue(Type, Id, Parent);
        RawId = id.ToString("n", CultureInfo.InvariantCulture);
    }

    public Subject(Guid id, string type, Guid? parentId = null)
    {
        Id = id;
        Parent = parentId;
        Type = type;
        Value = FormatValue(Type, Id, Parent);
        RawId = id.ToString("n", CultureInfo.InvariantCulture);
    }

    public Subject(string id, string type, string? parentId = null)
    {
        Id = id.ToGuid();
        Parent = string.IsNullOrEmpty(parentId) ? null : parentId.ToGuid();
        Type = type;
        Value = FormatValue(Type, id, parentId);
        RawId = id;
    }

    public Guid Id { get; }
    public Guid? Parent { get; }
    public string Type { get; }

    [JsonIgnore] public string RawId { get; }

    public string Value { get; }

    public static Subject ForAggregate(IAggregateRoot aggregate)
    {
        return ForAggregate(aggregate, aggregate.Id, null);
    }

    public static Subject ForAggregate(IAggregateRoot aggregate, Guid explicitId)
    {
        return ForAggregate(aggregate, explicitId, null);
    }

    public static Subject ForAggregate<TAggregate>(Guid explicitId)
        where TAggregate : IAggregateRoot
    {
        return new Subject(explicitId, typeof(TAggregate).Name);
    }

    public static Subject ForAggregate(IAggregateRoot aggregate, Guid explicitId, Guid? parentId)
    {
        return new Subject(explicitId, aggregate.GetType().Name, parentId);
    }

    public string ForStorage()
    {
        return FormatValue(Type, Id).Replace("/", "-");
    }

    private string FormatValue(string type, Guid id, Guid? parentId = null)
    {
        return FormatValue(
            type,
            id.ToString("n", CultureInfo.InvariantCulture),
            parentId.HasValue && parentId.Value != Guid.Empty
                ? $"/{parentId.Value.ToString("n", CultureInfo.InvariantCulture)}"
                : "");
    }

    private string FormatValue(string type, string id, string? parentId = null)
    {
        return
            $"{type}{parentId}/{id}";
    }

    private void Parse(string value, out Guid id, out Guid? parentId, out string type)
    {
        type = "NOTSET";
        id = Guid.Empty;
        parentId = null;

        var match = Regex.Match(value, Pattern);
        if (!match.Success)
            throw new ArgumentException("The value was formatted incorrectly for Subject.");

        id = Guid.Parse(match.Groups[^1].Value);
        type = match.Groups[1].Value;
        if (match.Groups[3].Captures.Count == 1)
            parentId = Guid.Parse(match.Groups[3].Value);
    }
}