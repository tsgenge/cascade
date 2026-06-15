using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.ReadModel.Projecting.Decorators;

internal class ProjectorTelemetryDecorator<TView> : IViewProjector<TView>
    where TView : class, IView
{
    private readonly IViewProjector<TView> _inner;
    private readonly ILogger<ProjectorTelemetryDecorator<TView>> _logger;
    private readonly ITelemetryLogger _telemetryLogger;

    public ProjectorTelemetryDecorator(ITelemetryLogger telemetryLogger,
        ILogger<ProjectorTelemetryDecorator<TView>> logger, IViewProjector<TView> inner)
    {
        _telemetryLogger = telemetryLogger ?? throw new ArgumentNullException(nameof(telemetryLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ProjectionResult<TView>> ProjectAsync(EventEnvelope @event)
    {
        using var op =
            _telemetryLogger.StartOperation($"PROJECTING /{typeof(TView).Name}/{@event.Type}/{@event.Sequence}");
        try {
            var outcome = await _inner.ProjectAsync(@event);
            _telemetryLogger.AddCustomEvent(Metrics.ProjectionOutcome,
                new Dictionary<string, string>
                {
                    { "Outcome", outcome.Outcome.ToString() }, { "SubjectId", @event.Subject.Value }
                });
            return outcome;
        }
        catch (Exception e) {
            _logger.LogError(e, "Failed to project event {Event} to view {View}", @event, typeof(TView).Name);
            throw;
        }
    }
}