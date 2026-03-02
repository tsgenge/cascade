namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

public interface IPartitionedContainer<TCollection> : IPagedContainer<TCollection>
    where TCollection : IDocumentContainerDefinition
{
    Task AddAsync<TDoc>(TDoc document) where TDoc : IDocument;
    Task AddBatchAsync<TDoc>(IList<TDoc> documents) where TDoc : IDocument;
    Task UpsertBatchAsync<TDoc>(IList<TDoc> documents) where TDoc : IDocument;
    Task DeleteAsync<TDoc>(Guid key, string partitionKey) where TDoc : IDocument;
    Task<TDoc?> GetAsync<TDoc>(Guid key, string partitionKey) where TDoc : IDocument;
    Task UpsertAsync<TDoc>(TDoc document) where TDoc : IDocument;
}