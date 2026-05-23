using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using Microsoft.Azure.Cosmos;
using System.Text;
using System.Text.Json;

namespace CascadeEsdm.Storage.CosmosDb;

public class CosmosJsonNetSerializer : CosmosSerializer
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
    private readonly JsonSerializerOptions _serializerSettings;

    public CosmosJsonNetSerializer()
        : this(DefaultSerialisationSettings.UsingTypeQualifiedName()) { }

    public CosmosJsonNetSerializer(
        JsonSerializerOptions? serializerSettings
    )
    {
        _serializerSettings = serializerSettings ?? DefaultSerialisationSettings.UsingTypeQualifiedName();
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream) {
            var streamType = typeof(Stream);
            if (typeof(Stream).IsAssignableFrom(typeof(T))) {
                return (T)(object)stream;
            }

            return JsonSerializer.Deserialize<T>(stream, _serializerSettings);
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var streamPayload = new MemoryStream();

        JsonSerializer.Serialize(streamPayload, input, _serializerSettings);

        streamPayload.Position = 0;
        return streamPayload;
    }
}