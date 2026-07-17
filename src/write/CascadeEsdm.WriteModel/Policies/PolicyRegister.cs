namespace CascadeEsdm.WriteModel.Policies;

internal sealed class PolicyRegister
{
    public PolicyRegister(string? key, Type policyType)
    {
        Key = key;
        PolicyType = policyType ?? throw new ArgumentNullException(nameof(policyType));
    }

    public string? Key { get; }

    public Type PolicyType { get; }
}
