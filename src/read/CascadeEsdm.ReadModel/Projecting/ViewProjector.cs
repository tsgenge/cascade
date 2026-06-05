using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

internal class ViewProjector<TView> : IViewProjector<TView>
    where TView : class, IView
{
    private readonly IAuthorResolver? _authorResolver;
    private readonly IEventCapabilityEvaluator<TView> _eventEvaluator;
    private readonly IViewEventMapper<TView> _eventMapper;
    private readonly IViewProjectionStore<TView> _projectionStore;
    private readonly IViewSequenceStore<TView> _sequenceStore;

    public ViewProjector(
        IEventCapabilityEvaluator<TView> eventEvaluator,
        IViewEventMapper<TView> eventMapper,
        IViewProjectionStore<TView> projectionStore,
        IViewSequenceStore<TView> sequenceStore,
        IAuthorResolver? authorResolver = null)
    {
        _eventEvaluator = eventEvaluator ?? throw new ArgumentNullException(nameof(eventEvaluator));
        _eventMapper = eventMapper ?? throw new ArgumentNullException(nameof(eventMapper));
        _projectionStore = projectionStore ?? throw new ArgumentNullException(nameof(projectionStore));
        _sequenceStore = sequenceStore ?? throw new ArgumentNullException(nameof(sequenceStore));
        _authorResolver = authorResolver;
    }

    public async Task<ProjectionResult<TView>> ProjectAsync(EventEnvelope @event)
    {
        var lastSequence = await _sequenceStore.GetLastSequenceAsync(@event.Subject);

        if (@event.Sequence < lastSequence.Value)
            return new ProjectionResult<TView>(ProjectionOutcome.Stale);

        if (@event.Sequence > lastSequence.Value + 1)
            return new ProjectionResult<TView>(ProjectionOutcome.Replay, lastSequence.UtcWhen);

        lastSequence = new Sequence(@event.Subject, @event.Time, @event.Sequence);

        if (_eventEvaluator.Supports(@event))
        {
            var allEffected = new List<Projection<TView>>();

            if (_eventEvaluator.RemovesRow(@event))
            {
                allEffected.AddRange(await _projectionStore.DeleteAsync(@event));
            }
            else
            {
                var (rows, partition) = await _projectionStore.GetRowsAsync(@event);

                if (rows.Count == 0 && _eventEvaluator.AddsRow(@event))
                {
                    var newRow = Activator.CreateInstance<TView>();
                    newRow.Id = _eventMapper.GetNewRowId(@event);

                    if (newRow is IAuthoredView authored && _authorResolver != null)
                    {
                        var identity = await _authorResolver.ResolveAsync(@event.SecurityContext);
                        if (identity != null)
                            authored.Author = identity;
                    }

                    if (@event.Subject.Parent.HasValue)
                        newRow.ParentId = @event.Subject.Parent;

                    rows = new List<TView> { newRow };
                    allEffected.Add(new Projection<TView>(ProjectionEffect.Added, newRow, partition));
                }
                else
                {
                    allEffected.AddRange(
                        rows.Select(r => new Projection<TView>(ProjectionEffect.Changed, r, partition)));
                }

                foreach (var row in rows)
                {
                    _eventMapper.Map(row, @event);
                }

                await _projectionStore.SaveAsync(rows, @event);
            }

            await _sequenceStore.SaveAsync(lastSequence);

            return new ProjectionResult<TView>(
                allEffected.Count > 0 ? ProjectionOutcome.Success : ProjectionOutcome.RecordNotFound,
                allEffected);
        }

        await _sequenceStore.SaveAsync(lastSequence);

        return new ProjectionResult<TView>(ProjectionOutcome.NotApplicable);
    }
}
