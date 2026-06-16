using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Exceptions;

namespace CascadeEsdm.WriteModel.Policies;

internal class PolicyDispatcher : IPolicyDispatcher
{
    private readonly IEnumerable<IPolicy> _policies;

    public PolicyDispatcher(IEnumerable<IPolicy> policies)
    {
        _policies = policies ?? throw new ArgumentNullException(nameof(policies));
    }

    public async Task DispatchAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null) throw new ArgumentNullException(nameof(envelope));

        var supportingPolicies = _policies.Where(p => p.Supports(envelope)).ToList();
        if (supportingPolicies.Count == 0)
            return;

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
