using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

public interface IPagedContainer<TCollection> where TCollection : IDocumentContainerDefinition
{
    Task<PagedResult<TDoc>> GetPageAsync<TDoc>(PartitionedPageQuery pageQuery) where TDoc : IDocument;
}