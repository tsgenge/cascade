using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Resolves which aggregate type handles each event by scanning IEventApplier implementations.
/// </summary>
public static class AggregateResolver
{
    /// <summary>
    /// Builds a map of event type name → aggregate type name by scanning applier classes.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildEventToAggregateMap(IEnumerable<ScannedEventFile> eventFiles)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in eventFiles)
        {
            foreach (var applier in file.ApplierClasses)
            {
                var (eventName, aggregateName) = ExtractApplierTypes(applier);
                if (!string.IsNullOrEmpty(eventName) && !string.IsNullOrEmpty(aggregateName))
                {
                    map[eventName] = aggregateName;
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Extracts the event type and aggregate type from an IEventApplier<TEvent, TAggregate> base type.
    /// </summary>
    private static (string? EventName, string? AggregateName) ExtractApplierTypes(ClassDeclarationSyntax applierClass)
    {
        if (applierClass.BaseList == null)
            return (null, null);

        foreach (var baseType in applierClass.BaseList.Types)
        {
            var typeSyntax = baseType.Type;

            // Check for IEventApplier<EventType, AggregateType>
            if (typeSyntax is GenericNameSyntax genericName &&
                genericName.Identifier.Text == "IEventApplier" &&
                genericName.TypeArgumentList.Arguments.Count == 2)
            {
                var eventType = genericName.TypeArgumentList.Arguments[0].ToString();
                var aggregateType = genericName.TypeArgumentList.Arguments[1].ToString();
                return (eventType, aggregateType);
            }

            // Handle qualified names like CascadeEsdm.WriteModel.Hydration.IEventApplier<TEvent, TAggregate>
            if (typeSyntax is QualifiedNameSyntax qualifiedGeneric &&
                qualifiedGeneric.Right is GenericNameSyntax rightGeneric &&
                rightGeneric.Identifier.Text == "IEventApplier" &&
                rightGeneric.TypeArgumentList.Arguments.Count == 2)
            {
                var eventType = rightGeneric.TypeArgumentList.Arguments[0].ToString();
                var aggregateType = rightGeneric.TypeArgumentList.Arguments[1].ToString();
                return (eventType, aggregateType);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Determines the aggregate name for a given event, applying pluralisation to the resolved name.
    /// Resolution order:
    /// 1. IEventApplier map (strip "Aggregate" suffix, pluralise)
    /// 2. Closest IAggregateRoot by namespace proximity (strip "Aggregate" suffix, pluralise)
    /// 3. Namespace-based fallback (first segment after root namespace)
    /// </summary>
    public static string? GetAggregateForEvent(
        RecordDeclarationSyntax eventRecord,
        string sourceNamespace,
        string sourceRootNamespace,
        IReadOnlyDictionary<string, string> eventToAggregateMap,
        IReadOnlyList<AggregateRootInfo> aggregateRoots)
    {
        var eventName = eventRecord.Identifier.Text;

        // First: try to find from applier map
        if (eventToAggregateMap.TryGetValue(eventName, out var aggregateFromMap))
        {
            var stripped = StripAggregateSuffix(aggregateFromMap);
            return Pluraliser.Pluralise(stripped);
        }

        // Second: find closest IAggregateRoot by namespace proximity
        var closestRoot = FindClosestAggregateRoot(sourceNamespace, aggregateRoots);
        if (closestRoot != null)
        {
            var stripped = StripAggregateSuffix(closestRoot.ClassName);
            return Pluraliser.Pluralise(stripped);
        }

        // Fallback: parse from namespace (e.g., CascadeEsdm.TestDomain.People.Events → People)
        if (sourceNamespace.StartsWith(sourceRootNamespace, StringComparison.Ordinal))
        {
            var remainder = sourceNamespace[sourceRootNamespace.Length..].TrimStart('.');
            var segments = remainder.Split('.');
            if (segments.Length > 0 && !string.IsNullOrEmpty(segments[0]))
            {
                return segments[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the closest IAggregateRoot to the event's namespace by walking up the namespace tree.
    /// An aggregate root is a candidate only if the event namespace starts with the root's namespace
    /// (i.e., the root is in a parent/ancestor namespace). Among candidates, the most specific
    /// parent (longest namespace) wins. If tied, the first discovered wins.
    /// </summary>
    private static AggregateRootInfo? FindClosestAggregateRoot(
        string eventNamespace,
        IReadOnlyList<AggregateRootInfo> aggregateRoots)
    {
        if (aggregateRoots.Count == 0)
            return null;

        AggregateRootInfo? best = null;
        int bestLength = -1;

        foreach (var root in aggregateRoots)
        {
            // The root must be in a parent namespace (event namespace starts with root namespace)
            if (!IsAncestorNamespace(eventNamespace, root.Namespace))
                continue;

            // Prefer the most specific (longest) ancestor namespace
            if (root.Namespace.Length > bestLength)
            {
                bestLength = root.Namespace.Length;
                best = root;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns true if <paramref name="ancestorNamespace"/> is a proper ancestor of
    /// <paramref name="descendantNamespace"/> (the descendant starts with the ancestor followed by a dot).
    /// </summary>
    private static bool IsAncestorNamespace(string descendantNamespace, string ancestorNamespace)
    {
        if (descendantNamespace.Length <= ancestorNamespace.Length)
            return false;

        return descendantNamespace.StartsWith(ancestorNamespace, StringComparison.Ordinal)
            && descendantNamespace[ancestorNamespace.Length] == '.';
    }

    private static string StripAggregateSuffix(string aggregateName)
    {
        // Convert PersonAggregate → Person, OrderAggregate → Order, etc.
        if (aggregateName.EndsWith("Aggregate", StringComparison.Ordinal))
        {
            return aggregateName[..^"Aggregate".Length];
        }
        return aggregateName;
    }
}
