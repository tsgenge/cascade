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
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IncludeFields = true, WriteIndented = true
            };

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

    /// <summary>
    /// Serialisation settings for publishing <see cref="Events.EventEnvelope"/> messages to a message bus.
    ///
    /// The <c>$type</c> discriminator written for each <see cref="Events.IDomainEvent"/> is rewritten
    /// from the write-model assembly-qualified name to its schema assembly equivalent using the same
    /// deterministic suffix-strip rule as the EventExtractor tool.  No external configuration is required;
    /// the mapping is derived entirely from the event type itself.
    ///
    /// Consumers that hold the generated schema assembly as a dependency can deserialise the envelope
    /// without any knowledge of the write-model project structure.
    /// </summary>
    public static JsonSerializerOptions ForMessageBus()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IncludeFields = true, WriteIndented = true
        };

        options.Converters.Add(
            new PolymorphicTypeConverter<IDomainEvent>(
                new SerialisationTypeNameResolver(SchemaTypeNameMapper.RewriteToSchemaTypeName)));
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