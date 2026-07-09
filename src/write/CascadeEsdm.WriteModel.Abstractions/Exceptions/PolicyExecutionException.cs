using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class PolicyExecutionException : AggregateExceptionBase
{
    public PolicyExecutionException(PolicyFailures failures)
        : base(failures.ToString(), failures.Exceptions)
    {
    }
}

public record PolicyFailures
{
    private readonly IReadOnlyList<PolicyFailure> _failures;
    public PolicyFailures(IReadOnlyList<PolicyFailure> failures)
    {
        _failures = failures;
    }
    public override string ToString()
    {
        var policyNames = string.Join(", ", _failures.Select(f => f.PolicyName));
        return $"One or more policies failed to execute: {policyNames}";
    }

    public Exception[] Exceptions => _failures.Select(f => f.Exception).ToArray();
}

public record PolicyFailure
{
    public PolicyFailure(string policyName, Exception exception)
    {
        PolicyName = policyName;
        Exception = exception;
    }

    public string PolicyName { get; }
    public Exception Exception { get; }
}
