using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.Infrastructure.Serialisation;

internal interface ISerialisationTypeResolver
{
    string GetJsonName(Type type);
    Type GetType(string jsonName);
}

internal class SerialisationTypeNameResolver : ISerialisationTypeResolver
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();
    private readonly Func<string, string>? _updateTypeMethod;

    public SerialisationTypeNameResolver(Func<string, string>? updateTypeMethod = null)
    {
        _updateTypeMethod = updateTypeMethod;
    }

    public Type GetType(string jsonName)
    {
        if (_updateTypeMethod != null)
            jsonName = _updateTypeMethod(jsonName);

        if (TypeCache.TryGetValue(jsonName, out var type))
            return type;

        var resolved = Type.GetType(jsonName) ?? AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
            .FirstOrDefault(x => x.FullName == jsonName);

        if (resolved != null) {
            TypeCache.TryAdd(jsonName, resolved);
            return resolved;
        }

        throw new JsonException($"Could not deserialise the type ${jsonName}.");
    }

    public string GetJsonName(Type type)
    {
        return CleanFullQualified(type.AssemblyQualifiedName) ?? type.FullName ?? type.Name;
    }

    private string CleanFullQualified(string? fullyQualified)
    {
        if (!string.IsNullOrWhiteSpace(fullyQualified)) {
            var newName = RemoveAssemblyAttributes(fullyQualified);
            if (_updateTypeMethod != null)
                newName = _updateTypeMethod(newName);

            return newName;
        }

        return string.Empty;
    }

    private static string RemoveAssemblyAttributes(string typeName)
    {
        return Regex.Replace(
            typeName,
            @",\s*(Version|Culture|PublicKeyToken)\s*=[^,\]\[]*",
            string.Empty);
    }
}