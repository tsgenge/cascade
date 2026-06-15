using AutoMapper;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.ReadModel.Querying;
using CascadeEsdm.ReadModel.Utilities;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     Persistence gateway used by the projector to load, save, and delete view rows
///     during projection.
/// </summary>
internal interface IViewProjectionStore<TView>
    where TView : IView
{
    Task<(IList<TView> Rows, Partition Partition)> GetRowsAsync(EventEnvelope @event);
    Task<IReadOnlyList<Projection<TView>>> DeleteAsync(EventEnvelope @event);
    Task SaveAsync(IEnumerable<TView> rows, EventEnvelope @event);
}

internal class ViewProjectionStore<TView, TContainer> : IViewProjectionStore<TView>
    where TContainer : IDocumentContainerDefinition
    where TView : IView
{
    private readonly IDictionary<Guid, ViewDocument> _cache = new Dictionary<Guid, ViewDocument>();
    private readonly IMapper _mapper;
    private readonly IProjectionPartitionLocator<TView> _partitionLocator;
    private readonly IPartitionedContainer<TContainer> _store;

    public ViewProjectionStore(IPartitionedContainer<TContainer> store, IMapper mapper,
        IProjectionPartitionLocator<TView> partitionLocator)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _partitionLocator = partitionLocator ?? throw new ArgumentNullException(nameof(partitionLocator));
    }

    public async Task<(IList<TView> Rows, Partition Partition)> GetRowsAsync(EventEnvelope @event)
    {
        var queryInfo = GetQueryInfo(@event);
        var partition = _partitionLocator.GetPartition(@event);

        var page = await _store.GetPageAsync<ViewDocument>(new PartitionedPageQuery(
            new PageFilter(queryInfo.Query, 1000),
            partition.Value,
            queryInfo.Parameters));

        foreach (var doc in page.Page) {
            _cache.TryAdd(doc.View!.Id, doc);
        }

        return (page.Page.Select(p => p.View).OfType<TView>().ToList(), partition);
    }

    public async Task<IReadOnlyList<Projection<TView>>> DeleteAsync(EventEnvelope @event)
    {
        var queryInfo = GetDeleteInfo(@event);
        var partition = _partitionLocator.GetPartition(@event);

        var page = await _store.GetPageAsync<ViewDocument>(new PartitionedPageQuery(
            new PageFilter(queryInfo.Query, 1000),
            partition.Value,
            queryInfo.Parameters));

        foreach (var doc in page.Page) {
            await _store.DeleteAsync<ViewDocument>(doc.Id, doc.PartitionKey);
        }

        return page.Page
            .Where(v => v.View != null)
            .Select(v => new Projection<TView>(ProjectionEffect.Removed, (TView)v.View!, partition))
            .ToList();
    }

    public async Task SaveAsync(IEnumerable<TView> rows, EventEnvelope @event)
    {
        await _store.UpsertBatchAsync(ConvertToDocuments(rows, @event));
    }

    private QueryDefinition GetQueryInfo(EventEnvelope @event)
    {
        var rowLocator = GetRowLocator(@event);
        var queryParams = new Dictionary<string, string>
        {
            { "@subject", @event.Subject.Value }, { "@type", typeof(TView).Name }
        };
        var query = "select * from c where c.type = @type and ";

        if (rowLocator != null) {
            query +=
                $"({rowLocator.Operation.ToQueryString($"c.view.{rowLocator.PropertySelector.Key.ToCamelCase()}", "@selectValue")})";
            queryParams.Add("@selectValue", rowLocator.PropertySelector.Value.ToString());
        }
        else {
            query += "(array_contains(c.sources, @subject))";
        }

        return new QueryDefinition(query, queryParams);
    }

    private QueryDefinition GetDeleteInfo(EventEnvelope @event)
    {
        var rowLocator = GetRowLocator(@event)
                         ?? throw new ArgumentNullException(nameof(@event),
                             $"The delete mapping for event {@event.Event.GetType().Name} did not have a RowLocator defined.");

        var queryParams = new Dictionary<string, string> { { "@type", typeof(TView).Name } };

        var query =
            $"select * from c where c.type = @type and {rowLocator.Operation.ToQueryString($"c.view.{rowLocator.PropertySelector.Key.ToCamelCase()}", "@selectValue")}";
        queryParams.Add("@selectValue", rowLocator.PropertySelector.Value.ToString());

        return new QueryDefinition(query, queryParams);
    }

    private RowLocator<TView>? GetRowLocator(EventEnvelope @event)
    {
        try {
            return _mapper.Map<RowLocator<TView>>(@event.Event, o => o.State = @event);
        }
        catch (AutoMapperMappingException) {
            return null;
        }
    }

    private IList<ViewDocument> ConvertToDocuments(IEnumerable<TView> rows, EventEnvelope @event)
    {
        var partition = _partitionLocator.GetPartition(@event);
        return rows.Select(p =>
        {
            _cache.TryGetValue(p.Id, out var existing);

            if (existing != null) {
                existing.View = p;

                if (existing.Sources.All(s => !s.Equals(@event.Subject.Value)))
                    existing.Sources.Add(@event.Subject.Value);

                return existing;
            }

            return new ViewDocument
            {
                Id = p.Id,
                PartitionKey = partition.Value,
                Created = DateTime.UtcNow,
                View = p,
                Type = typeof(TView).Name,
                Sources = new List<string> { @event.Subject.Value }
            };
        }).ToList();
    }
}