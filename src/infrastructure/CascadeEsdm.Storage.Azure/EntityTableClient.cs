using Azure.Data.Tables;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using System.Reflection;
using ITableEntity = CascadeEsdm.SharedKernel.Infrastructure.Storage.ITableEntity;

namespace CascadeEsdm.Storage.Azure;

internal interface IEntityTableClient<TEntity>
{
    TableClient GetTableClient();
}

internal class EntityTableClient<TEntity> : IEntityTableClient<TEntity>
    where TEntity : ITableEntity
{
    private readonly TableClient _tableClient;

    public EntityTableClient(TableServiceClient client)
    {
        var tableNameAttribute = typeof(TEntity).GetCustomAttribute<TableNameAttribute>();
        var tableName = !string.IsNullOrWhiteSpace(tableNameAttribute?.Name)
            ? tableNameAttribute.Name
            : typeof(TEntity).Name.ToLower();

        _tableClient = client?.GetTableClient(tableName) ?? throw new ArgumentNullException(nameof(client));
    }

    public TableClient GetTableClient()
    {
        return _tableClient;
    }
}