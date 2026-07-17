using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.WriteModel.Policies;

internal class PolicyDispatcher : IPolicyDispatcher
{
    private readonly string? _key;
    private readonly ILogger<PolicyDispatcher> _logger;
    private readonly IEnumerable<PolicyRegister> _policyRegisters;
    private readonly IServiceScopeFactory _scopeFactory;

    public PolicyDispatcher(
        IServiceScopeFactory scopeFactory,
        string? key,
        IEnumerable<PolicyRegister> policyRegisters,
        ILogger<PolicyDispatcher> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _key = key;
        _policyRegisters = policyRegisters ?? throw new ArgumentNullException(nameof(policyRegisters));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (envelope is null) throw new ArgumentNullException(nameof(envelope));

        var policyTypes = _policyRegisters
            .Where(r => r.Key == _key)
            .Select(r => r.PolicyType)
            .ToList();

        var tasks = policyTypes
            .Select(policyType => ExecutePolicySafeAsync(policyType, envelope, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(tasks);

        if (results.All(r => !r.Supported)) {
            _logger.LogWarning("No supporting policies found for event {EventType} on subject {Subject}",
                envelope.Type, envelope.Subject);
            return;
        }

        var failures = results
            .Where(r => r.Supported && r.Failure is not null)
            .Select(r => r.Failure!)
            .ToList();

        if (failures.Count > 0)
            throw new PolicyExecutionException(new PolicyFailures(failures));
    }

    private async Task<PolicyExecutionResult> ExecutePolicySafeAsync(
        Type policyType, EventEnvelope envelope, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var policy = (IPolicy)scope.ServiceProvider.GetRequiredService(policyType);

        if (!policy.Supports(envelope))
            return new PolicyExecutionResult(false, null);

        using var logScope = _logger.BeginScope("Executing Policy: {PolicyName}", policy.GetType().Name);
        try {
            await policy.ExecuteAsync(envelope, cancellationToken);
            return new PolicyExecutionResult(true, null);
        }
        catch (Exception ex) {
            return new PolicyExecutionResult(true, new PolicyFailure(policy.GetType().Name, ex));
        }
    }

    private record PolicyExecutionResult
    {
        public PolicyExecutionResult(bool supported, PolicyFailure? failure)
        {
            Supported = supported;
            Failure = failure;
        }

        public bool Supported { get; }
        public PolicyFailure? Failure { get; }
    }
}