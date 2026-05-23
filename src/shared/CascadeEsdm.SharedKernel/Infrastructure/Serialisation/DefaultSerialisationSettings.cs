using CascadeEsdm.SharedKernel.Events;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.Infrastructure.Serialisation;

public static class DefaultSerialisationSettings
{
    public static JsonSerializerOptions UsingTypeQualifiedName()
    {
        var options =
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

        options.Converters.Add(new PolymorphicTypeConverter<IDomainEvent>());
        options.Converters.Add(new ValueObjectConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    public static JsonSerializerOptions ReplacingWithCustomAssemblyName(string assemblyName)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IncludeFields = true, WriteIndented = true
        };

        options.Converters.Add(
            new PolymorphicTypeConverter<IDomainEvent>(
                new SerialisationTypeNameResolver(s => ReplaceAssemblyComponent(s, assemblyName))));
        options.Converters.Add(new ValueObjectConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    private static string ReplaceAssemblyComponent(string original, string newAssemblyName)
    {
        var pattern = @"\,\s+?[\w\.]+$";
        if (Regex.IsMatch(original, pattern)) {
            return Regex.Replace(original, pattern, $", {newAssemblyName}");
        }

        return original;
    }
}