using CascadeEsdm.WriteModel.Policies;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CascadeEsdm.WriteModel.Composition;

public class PolicyBuilder
{
    private readonly IServiceCollection _services;
    private readonly string? _key;

    public PolicyBuilder(IServiceCollection services)
        : this(services, null)
    {
    }

    public PolicyBuilder(IServiceCollection services, string? key)
    {
        _services = services;
        _key = key;
    }

    public PolicyBuilder AddPolicy<TPolicy>()
        where TPolicy : class, IPolicy
    {
        if (_key is null)
            _services.AddScoped<IPolicy, TPolicy>();
        else
            _services.AddKeyedScoped<IPolicy, TPolicy>(_key);
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
            if (_key is null)
                _services.AddScoped(typeof(IPolicy), policyType);
            else
                _services.AddKeyedScoped(typeof(IPolicy), _key, policyType);
        }
    }

    private static IEnumerable<Type> GetPolicyTypes(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IPolicy).IsAssignableFrom(t));
    }
}
