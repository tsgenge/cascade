using CascadeEsdm.WriteModel.Policies;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CascadeEsdm.WriteModel.Composition;

public class PolicyBuilder
{
    private readonly IServiceCollection _services;

    public PolicyBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public PolicyBuilder AddPolicy<TPolicy>()
        where TPolicy : class, IPolicy
    {
        _services.AddScoped<IPolicy, TPolicy>();
        return this;
    }

    public PolicyBuilder AddPoliciesFromNamespace<TExampleType>()
    {
        var targetNamespace = typeof(TExampleType).Namespace
            ?? throw new ArgumentException($"Type {typeof(TExampleType).Name} has no namespace.");

        var policyTypes = GetPolicyTypes(typeof(TExampleType).Assembly)
            .Where(t => t.Namespace != null && t.Namespace.StartsWith(targetNamespace, StringComparison.Ordinal));

        RegisterPolicies(policyTypes);
        return this;
    }

    public PolicyBuilder AddPoliciesFromAssembly<TExampleType>()
    {
        var policyTypes = GetPolicyTypes(typeof(TExampleType).Assembly);
        RegisterPolicies(policyTypes);
        return this;
    }

    private void RegisterPolicies(IEnumerable<Type> policyTypes)
    {
        foreach (var policyType in policyTypes) {
            _services.AddScoped(typeof(IPolicy), policyType);
        }
    }

    private static IEnumerable<Type> GetPolicyTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IPolicy).IsAssignableFrom(t));
    }
}
