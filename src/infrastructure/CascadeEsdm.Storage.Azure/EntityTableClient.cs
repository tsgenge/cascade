using System.Reflection;
using Azure.Data.Tables;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;

namespace CascadeEsdm.Storage.Azure;

internal interface IEntityTableClient<TEntity>
{
    TableClient GetTableClient();
}

internal class EntityTableClient<TEntity> : IEntityTableClient<TEntity>
    where TEntity : CascadeEsdm.SharedKernel.Infrastructure.Storage.ITableEntity
{
    private readonly TableClient _tableClient;

    public EntityTableClient(TableServiceClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var tableNameAttribute = typeof(TEntity).GetCustomAttribute<TableNameAttribute>();
        var tableName = !string.IsNullOrWhiteSpace(tableNameAttribute?.Name)
            ? tableNameAttribute.Name
            : typeof(TEntity).Name.ToLower();

        _tableClient = client.GetTableClient(tableName);
    }

    public TableClient GetTableClient() => _tableClient;
}
