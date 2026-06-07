using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.ReadModel.Views;
using System.Text.Json.Serialization;

namespace CascadeEsdm.ReadModel.Projecting;

internal class ViewDocument : IDocument
{
    public Guid Id { get; set; }
    public string PartitionKey { get; set; } = string.Empty;
    public IView? View { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset Created { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<Guid> Groups { get; set; } = new();
    [JsonPropertyName("_etag")]
    public string ETag { get; set; } = "*";
    [JsonPropertyName("_ts")]
    public long? Timestamp { get; set; }
}
