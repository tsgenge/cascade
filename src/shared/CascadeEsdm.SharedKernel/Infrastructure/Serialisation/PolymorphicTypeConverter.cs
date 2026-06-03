using CascadeEsdm.SharedKernel.ValueObjects;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeEsdm.SharedKernel.Infrastructure.Serialisation;

internal class PolymorphicTypeConverter<TType> : JsonConverter<TType>
{
    private readonly ISerialisationTypeResolver _typeResolver;

    public PolymorphicTypeConverter(ISerialisationTypeResolver? typeResolver = null)
    {
        _typeResolver = typeResolver ?? new SerialisationTypeNameResolver();
    }

    public override TType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) {
            throw new JsonException("Expected StartObject token");
        }

        // Read the object, including the $type property
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var rootElement = jsonDocument.RootElement;

        var typeName = GetTypeName(rootElement);

        // Find the type using the $type property value
        var type = _typeResolver.GetType(typeName);
        if (type == null || !typeof(TType).IsAssignableFrom(type)) {
            throw new JsonException($"Unknown type: {typeName}");
        }

        // Deserialize the object into the correct type using the provided JsonSerializerOptions
        // This will ensure nested objects are handled with any additional converters specified in the options.
        var cleanedOptions = CleanOptions(options);

        var resultObject = (TType)JsonSerializer.Deserialize(rootElement.GetRawText(), type, cleanedOptions);

        return resultObject;
    }

    private JsonSerializerOptions CleanOptions(JsonSerializerOptions options)
    {
        var cleanedOptions = new JsonSerializerOptions(options);

        var converter = cleanedOptions.Converters.FirstOrDefault(t => t.GetType() == GetType());
        if (converter != null) {
            cleanedOptions.Converters.Remove(converter);
        }

        return cleanedOptions;
    }

    private static string GetTypeName(JsonElement rootElement)
    {
        string? typeName = null;
        foreach (var element in rootElement.EnumerateObject()) {
            if (element.Name == "$type") {
                typeName = element.Value.GetString();
                if (typeName == null) {
                    throw new JsonException("Missing $type property or invalid type name");
                }

                break;
            }
        }

        if (typeName == null) {
            throw new JsonException("Missing $type property");
        }

        return typeName;
    }

    public override void Write(Utf8JsonWriter writer, TType value, JsonSerializerOptions options)
    {
        // Start writing the object
        writer.WriteStartObject();

        // Add the $type property with the full type name
        writer.WriteString("$type", _typeResolver.GetJsonName(value.GetType()));

        // Use the JsonSerializer to handle all the properties, including any nested objects.
        // It will use the converters specified in the options to handle other types.

        var cleanedOptions = CleanOptions(options);

        foreach (var prop in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (prop.CanRead && prop.GetIndexParameters().Length == 0) {
                var propName = options.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
                var propValue = prop.GetValue(value);
                writer.WritePropertyName(propName);
                JsonSerializer.Serialize(writer, propValue, propValue?.GetType() ?? typeof(object), cleanedOptions);
            }
        }

        writer.WriteEndObject();
    }

    public override bool CanConvert(Type typeToConvert)
    {
        // Use IsAssignableFrom instead of IsAssignableTo for .NET Standard 2.1 compatibility
        // IsAssignableTo was introduced in .NET 5, but IsAssignableFrom has been available since .NET Framework 1.1
        return typeof(TType).IsAssignableFrom(typeToConvert) && !typeof(IValueObject).IsAssignableFrom(typeToConvert);
    }
}