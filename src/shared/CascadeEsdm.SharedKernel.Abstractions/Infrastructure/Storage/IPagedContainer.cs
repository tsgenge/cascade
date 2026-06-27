using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.SharedKernel.Infrastructure.Storage;

public interface IPagedContainer<TCollection> where TCollection : IDocumentContainerDefinition
{
    Task<PageResult<TDoc>> GetPageAsync<TDoc>(PartitionedPageQuery pageQuery) where TDoc : IDocument;
}