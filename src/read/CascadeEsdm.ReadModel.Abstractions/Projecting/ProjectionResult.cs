using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting;

/// <summary>
///     The outcome of projecting a single event into a view, along with any rows that were affected.
/// </summary>
public record ProjectionResult<TView>
    where TView : IView
{
    public ProjectionResult(ProjectionOutcome outcome, IReadOnlyList<Projection<TView>> effected,
        DateTimeOffset? replayTime = null)
    {
        Outcome = outcome;
        Effected = effected;
        ReplayTime = replayTime;
    }

    public ProjectionResult(ProjectionOutcome outcome, DateTimeOffset? replayTime = null)
        : this(outcome, new List<Projection<TView>>(), replayTime)
    {
    }

    public ProjectionOutcome Outcome { get; }
    public IReadOnlyList<Projection<TView>> Effected { get; }
    public DateTimeOffset? ReplayTime { get; }

    public bool ShouldChainToNextProjector =>
        Outcome != ProjectionOutcome.Faulted && Outcome != ProjectionOutcome.Replay;

    public string ToMessage()
    {
        return Outcome switch
        {
            ProjectionOutcome.Replay => $"[{typeof(TView).Name}] Replay requested, but don't know how.",
            ProjectionOutcome.Stale => $"[{typeof(TView).Name}] Message was applicable to this view, but it was stale.",
            ProjectionOutcome.NotApplicable => $"[{typeof(TView).Name}] Message was not applicable for this view, skipping.",
            ProjectionOutcome.Faulted => $"[{typeof(TView).Name}] There was an error from the Projector, the event should be deadlettered.",
            ProjectionOutcome.Success => $"[{typeof(TView).Name}] The event was processed successfully.",
            ProjectionOutcome.RecordNotFound => $"[{typeof(TView).Name}] The view record was not found.",
            _ => $"Unexpected projection outcome ({Outcome})."
        };
    }
}

/// <summary>
///     The outcome of a projection attempt.
/// </summary>
public enum ProjectionOutcome
{
    Success,
    Stale,
    NotApplicable,
    Replay,
    Faulted,
    RecordNotFound
}
