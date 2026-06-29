using CascadeEsdm.SharedKernel.Exceptions;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace CascadeEsdm.Storage.CosmosDb;

internal class CosmosDbContainer<TContainer> : IPartitionedContainer<TContainer>
    where TContainer : IDocumentContainerDefinition
{
    private const string RruKey = "TotalCosmosDbRequestCharge = {0}";
    private readonly Container _container;
    private readonly IDocumentContainerDefinition _containerDefinition;
    private readonly ILogger _logger;

    public CosmosDbContainer(CosmosClient client, IOptions<CosmosOptions> cosmosOptions,
        ILogger<CosmosDbContainer<TContainer>> logger)
    {
        _logger = logger;
        var db = client.GetDatabase(cosmosOptions.Value.DatabaseName);
        _containerDefinition = GetContainerAttribute();
        _container = db.GetContainer(_containerDefinition.Name);
    }

    public async Task<PageResult<TDoc>> GetPageAsync<TDoc>(PartitionedPageQuery pageQuery) where TDoc : IDocument
    {
        var query = string.IsNullOrWhiteSpace(pageQuery.Query) ? "select * from c" : pageQuery.Query;
        var queryDefinition = new QueryDefinition(query);
        if (pageQuery.QueryParameters.Keys.Count > 0) {
            foreach (var p in pageQuery.QueryParameters) {
                queryDefinition.WithParameter(p.Key, p.Value);
            }
        }

        return await GetPageResultsAsync<TDoc>(queryDefinition, pageQuery);
    }

    public async Task AddAsync<TDoc>(TDoc document) where TDoc : IDocument
    {
        try {
            var response = await _container.CreateItemAsync(document);
            _logger.LogInformation(RruKey, response.RequestCharge);
        }
        catch (CosmosException ex) {
            if (ex.StatusCode == HttpStatusCode.Conflict)
                throw new ConflictException("The entity already exists.");
            throw;
        }
    }

    public async Task AddBatchAsync<TDoc>(IList<TDoc> documents) where TDoc : IDocument
    {
        if (!documents.Any())
            return;

        var batch = _container.CreateTransactionalBatch(new PartitionKey(documents.First().PartitionKey));
        foreach (var doc in documents) {
            batch.CreateItem(doc, new TransactionalBatchItemRequestOptions());
        }

        var response = await batch.ExecuteAsync();
        _logger.LogInformation(RruKey, response.RequestCharge);
    }

    public async Task UpsertBatchAsync<TDoc>(IList<TDoc> documents) where TDoc : IDocument
    {
        if (!documents.Any())
            return;

        var batch = _container.CreateTransactionalBatch(new PartitionKey(documents.First().PartitionKey));
        foreach (var doc in documents) {
            batch.UpsertItem(doc, new TransactionalBatchItemRequestOptions());
        }

        var response = await batch.ExecuteAsync();
        _logger.LogInformation(RruKey, response.RequestCharge);
    }

    public async Task UpsertAsync<TDoc>(TDoc document) where TDoc : IDocument
    {
        var response = await _container.UpsertItemAsync(document);
        _logger.LogInformation(RruKey, response.RequestCharge);
    }

    public async Task<TDoc?> GetAsync<TDoc>(Guid key, string partitionKey) where TDoc : IDocument
    {
        try {
            var response = await _container.ReadItemAsync<TDoc>(key.ToString(), new PartitionKey(partitionKey));
            _logger.LogInformation(RruKey, response.RequestCharge);
            return response.Resource;
        }
        catch (CosmosException ex) {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return default;
            throw;
        }
    }

    public async Task DeleteAsync<TDoc>(Guid key, string partitionKey) where TDoc : IDocument
    {
        var response = await _container.DeleteItemAsync<TDoc>(key.ToString(), new PartitionKey(partitionKey));
        _logger.LogInformation(RruKey, response.RequestCharge);
    }

    private TContainer GetContainerAttribute()
    {
        return Activator.CreateInstance<TContainer>();
    }

    private async Task<PageResult<TDoc>> GetPageResultsAsync<TDoc>(QueryDefinition queryDefinition,
        PartitionedPageQuery pageQuery) where TDoc : IDocument
    {
        var results = new List<TDoc>();
        var continuationToken = string.Empty;

        try {
            using (var resultSetIterator = _container.GetItemQueryIterator<TDoc>(
                       queryDefinition, pageQuery.ContinuationToken,
                       new QueryRequestOptions
                       {
                           MaxItemCount = Math.Max(pageQuery.Size, 50),
                           PartitionKey = new PartitionKey(pageQuery.PartitionKey)
                       })) {
                while (resultSetIterator.HasMoreResults) {
                    var response = await resultSetIterator.ReadNextAsync();
                    _logger.LogInformation(RruKey, response.RequestCharge);
                    results.AddRange(response);
                    continuationToken = response.ContinuationToken;
                }
            }
        }
        catch (Exception ex) {
            throw new Exception("Unable to load from CosmosDb.", ex);
        }

        return new PageResult<TDoc>(results, new PageContinuationToken(continuationToken));
    }
}