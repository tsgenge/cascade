using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.ReadModel.Projecting.Decorators;

internal class ProjectorReplayDecorator<TView> : IViewProjector<TView>
    where TView : class, IView
{
    private readonly IViewProjector<TView> _inner;
    private readonly ILogger<ProjectorReplayDecorator<TView>> _logger;

    public ProjectorReplayDecorator(IViewProjector<TView> inner, ILogger<ProjectorReplayDecorator<TView>> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProjectionResult<TView>> ProjectAsync(EventEnvelope @event)
    {
        var result = await _inner.ProjectAsync(@event);

        if (result.Outcome == ProjectionOutcome.Replay)
            _logger.LogError("The view {View} is out of sequence and requires a replay.", typeof(TView).Name);

        return result;
    }
}