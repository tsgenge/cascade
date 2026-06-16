using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Exceptions;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.WriteModel.Policies;

internal class PolicyDispatcher : IPolicyDispatcher
{
    private readonly IEnumerable<IPolicy> _policies;
    private readonly ILogger<PolicyDispatcher> _logger;

    public PolicyDispatcher(IEnumerable<IPolicy> policies, ILogger<PolicyDispatcher> logger)
    {
        _policies = policies ?? throw new ArgumentNullException(nameof(policies));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null) throw new ArgumentNullException(nameof(envelope));

        var supportingPolicies = _policies.Where(p => p.Supports(envelope)).ToList();
        if (supportingPolicies.Count == 0) {
            _logger.LogWarning("No supporting policies found for event {EventType} on subject {Subject}",
                envelope.Type, envelope.Subject);
            return;
        }

        var tasks = supportingPolicies
            .Select(policy => ExecutePolicySafeAsync(policy, envelope, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks);

        var failures = results.Where(r => r is not null).ToList();
        if (failures.Count > 0)
            throw new PolicyExecutionException(failures!);
    }

    private static async Task<PolicyFailure?> ExecutePolicySafeAsync(
        IPolicy policy, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        try {
            await policy.ExecuteAsync(envelope, cancellationToken);
            return null;
        }
        catch (Exception ex) {
            return new PolicyFailure(policy.GetType().Name, ex);
        }
    }
}
