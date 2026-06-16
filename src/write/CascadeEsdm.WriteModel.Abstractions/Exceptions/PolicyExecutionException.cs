using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class PolicyExecutionException : ExceptionBase
{
    public PolicyExecutionException(IReadOnlyList<PolicyFailure> failures)
        : base(FormatMessage(failures))
    {
        Failures = failures;
    }

    public IReadOnlyList<PolicyFailure> Failures { get; }

    private static string FormatMessage(IReadOnlyList<PolicyFailure> failures)
    {
        var policyNames = string.Join(", ", failures.Select(f => f.PolicyName));
        return $"One or more policies failed to execute: {policyNames}";
    }
}

public class PolicyFailure
{
    public PolicyFailure(string policyName, Exception exception)
    {
        PolicyName = policyName;
        Exception = exception;
    }

    public string PolicyName { get; }
    public Exception Exception { get; }
}
