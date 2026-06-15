using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.ReadModel.Projecting.Decorators;

internal class ProjectorConcurrencyDecorator<TView> : IViewProjector<TView>
    where TView : class, IView
{
    private readonly IViewCapabilityEvaluator<TView> _capabilityEvaluator;
    private readonly IViewProjector<TView> _inner;
    private readonly IDistributedLockProvider _lockProvider;
    private readonly ILogger _logger;

    public ProjectorConcurrencyDecorator(IViewCapabilityEvaluator<TView> capabilityEvaluator,
        IViewProjector<TView> inner, IDistributedLockProvider lockProvider,
        ILogger<ProjectorConcurrencyDecorator<TView>> logger)
    {
        _capabilityEvaluator = capabilityEvaluator ?? throw new ArgumentNullException(nameof(capabilityEvaluator));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProjectionResult<TView>> ProjectAsync(EventEnvelope @event)
    {
        await using var lk = await AcquireLockIfRequiredAsync(@event);

        return await _inner.ProjectAsync(@event);
    }

    private async Task<IDistributedLock?> AcquireLockIfRequiredAsync(EventEnvelope @event)
    {
        if (_capabilityEvaluator.IsMultiAggregateView()) {
            _logger.LogInformation("Acquiring lock for view {ViewType} with subject {Subject}", typeof(TView).Name,
                @event.Subject.Value);
            return await _lockProvider.AcquireLockAsync(GetLockName(@event));
        }

        _logger.LogInformation("View {ViewType} is not a multi-aggregate view, skipping lock acquisition",
            typeof(TView).Name);

        return null;
    }

    private string GetLockName(EventEnvelope @event)
    {
        return $"{typeof(TView).Name.ToLower()}/{@event.Subject.Value}";
    }
}