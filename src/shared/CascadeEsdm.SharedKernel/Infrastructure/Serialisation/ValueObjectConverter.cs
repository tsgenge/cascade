using CascadeEsdm.SharedKernel.ValueObjects;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CascadeEsdm.SharedKernel.Infrastructure.Serialisation;

public class ValueObjectConverter : JsonConverter<IValueObject>
{
    public override bool CanConvert(Type typeToConvert)
    {
        // The converter applies to all types that implement IValueObject<TValue>
        return typeof(IValueObject).IsAssignableFrom(typeToConvert);
    }

    public override IValueObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Use reflection to determine the generic type argument (TValue)
        var valueObjectType = typeToConvert;
        var valueType = valueObjectType.GetInterface("IValueObject`1")?.GenericTypeArguments[0];

        // Deserialize the value
        var value = JsonSerializer.Deserialize(ref reader, valueType, options);

        // Find the constructor that takes a TValue argument
        var constructor = valueObjectType.GetConstructor(new[] { valueType });
        if (constructor == null) {
            throw new JsonException($"No suitable constructor found for {typeToConvert.FullName}.");
        }

        // Create the value object using the deserialized value
        return (IValueObject)constructor.Invoke(new[] { value });
    }

    public override void Write(Utf8JsonWriter writer, IValueObject valueObject, JsonSerializerOptions options)
    {
        // Get the value type (TValue)
        var valueType = valueObject.GetType().GetInterface("IValueObject`1")?.GenericTypeArguments[0];

        // Serialize the value
        var valueProperty = valueObject.GetType().GetProperty("Value");
        var value = valueProperty.GetValue(valueObject);
        JsonSerializer.Serialize(writer, value, valueType, options);
    }
}