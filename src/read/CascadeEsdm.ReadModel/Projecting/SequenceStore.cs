using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

internal class SequenceStore<TView> : IViewSequenceStore<TView>
    where TView : IView
{
    private readonly IDictionary<string, SequenceEntity> _cache = new Dictionary<string, SequenceEntity>();
    private readonly ITableStore<SequenceEntity> _table;
    private readonly string _viewName;

    public SequenceStore(ITableStore<SequenceEntity> table)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _viewName = typeof(TView).Name;
    }

    public async Task<Sequence> GetLastSequenceAsync(Subject subject)
    {
        if (!_cache.TryGetValue(subject.Value, out var existing))
        {
            existing = await _table.GetAsync(FormPartitionKey(), FormRowKey(subject));
            if (existing != null)
                _cache.Add(subject.Value, existing);
        }

        if (existing != null)
            return new Sequence(subject, existing.When, existing.Value);

        return Sequence.Initial(subject);
    }

    public async Task SaveAsync(Sequence lastSequence)
    {
        var etag = _cache.ContainsKey(lastSequence.Subject.Value)
            ? _cache[lastSequence.Subject.Value].ETag
            : "*";

        await _table.UpsertAsync(new SequenceEntity
        {
            RowKey = FormRowKey(lastSequence.Subject),
            PartitionKey = FormPartitionKey(),
            When = DateTimeOffset.UtcNow,
            Value = lastSequence.Value,
            ETag = etag
        });

        _cache.Clear();
    }

    private string FormPartitionKey() => _viewName.ToLower();

    private string FormRowKey(Subject subject) => subject.ForStorage();

    [TableName(Name = "sequences")]
    internal class SequenceEntity : ITableEntity
    {
        public string RowKey { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? Timestamp { get; set; }
        public long Value { get; set; }
        public string ETag { get; set; } = string.Empty;
    }
}
