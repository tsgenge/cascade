namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

public interface ITableStore<TEntity>
    where TEntity : class, ITableEntity
{
    Task<TEntity?> GetAsync(string partitionId, string rowId);
    Task UpsertAsync(TEntity entity);
}
