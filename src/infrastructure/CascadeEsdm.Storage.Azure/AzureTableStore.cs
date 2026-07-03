using System.Text.Json;
using Microsoft.Extensions.Logging;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;

namespace CascadeEsdm.Storage.Azure;

internal class AzureTableStore<TEntity> : ITableStore<TEntity>
    where TEntity : class, CascadeEsdm.SharedKernel.Infrastructure.Storage.ITableEntity
{
    private readonly global::Azure.Data.Tables.TableClient _tableClient;
    private readonly ILogger<AzureTableStore<TEntity>> _logger;

    public AzureTableStore(IEntityTableClient<TEntity> tableClient, ILogger<AzureTableStore<TEntity>> logger)
    {
        _tableClient = tableClient?.GetTableClient() ?? throw new ArgumentNullException(nameof(tableClient));
        _logger = logger;
    }

    public async Task<TEntity?> GetAsync(string partitionId, string rowId)
    {
        var row = await _tableClient.GetEntityIfExistsAsync<EmbeddedEntity>(partitionId, rowId);

        if (row.HasValue && !string.IsNullOrWhiteSpace(row.Value!.Payload))
        {
            try
            {
                var payload = JsonSerializer.Deserialize<TEntity>(row.Value.Payload);
                if (payload != null)
                {
                    payload.PartitionKey = row.Value.PartitionKey;
                    payload.RowKey = row.Value.RowKey;
                    payload.ETag = row.Value.ETag.ToString();
                }
                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to deserialize row contents for {PartitionId}/{RowId}", partitionId, rowId);
            }
        }

        return null;
    }

    public async Task UpsertAsync(TEntity entity)
    {
        var newRow = new EmbeddedEntity
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Payload = JsonSerializer.Serialize(entity, typeof(TEntity), new JsonSerializerOptions { WriteIndented = false }),
            ETag = new global::Azure.ETag(entity.ETag ?? "*")
        };

        await _tableClient.UpsertEntityAsync(newRow);
    }
}
