using CascadeEsdm.ReadModel.Infrastructure;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.ReadModel.Projecting.Decorators;

internal class ProjectorNotificationDecorator<TView> : IViewProjector<TView>
    where TView : class, IView
{
    private readonly ILogger<ProjectorNotificationDecorator<TView>> _logger;
    private readonly IViewNotificationService _notificationService;
    private readonly IViewProjector<TView> _projector;

    public ProjectorNotificationDecorator(IViewProjector<TView> projector,
        IViewNotificationService notificationService, ILogger<ProjectorNotificationDecorator<TView>> logger)
    {
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    public async Task<ProjectionResult<TView>> ProjectAsync(EventEnvelope @event)
    {
        var result = await _projector.ProjectAsync(@event);
        if (result.Outcome == ProjectionOutcome.Success) {
            using var scope =
                _logger.BeginScope("Notifying Client of Projection Success for {View}", typeof(TView).Name);
            await _notificationService.ViewChangedAsync(result.Effected, @event);
        }

        return result;
    }
}