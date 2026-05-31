using CascadeEsdm.SharedKernel.Composition;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCascadeEsdm(this IServiceCollection services, Action<CascadeBuilder> configure)
    {
        var builder = new CascadeBuilder(services);
        configure(builder);
        return services;
    }

    public static IServiceCollection AddGeneric(
        this IServiceCollection services,
        Type interfaceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (!interfaceType.IsGenericTypeDefinition) {
            throw new ArgumentException("Interface type must be a generic type definition.", nameof(interfaceType));
        }

        if (!implementationType.IsGenericTypeDefinition) {
            throw new ArgumentException("Implementation type must be a generic type definition.",
                nameof(implementationType));
        }

        var descriptor = new ServiceDescriptor(interfaceType, implementationType, lifetime);
        services.Add(descriptor);

        return services;
    }

    public static IServiceCollection AddScopedGeneric(
        this IServiceCollection services,
        Type interfaceType,
        Type implementationType)
    {
        return services.AddGeneric(interfaceType, implementationType);
    }

    public static IServiceCollection AddTransientGeneric(
        this IServiceCollection services,
        Type interfaceType,
        Type implementationType)
    {
        return services.AddGeneric(interfaceType, implementationType, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddSingletonGeneric(
        this IServiceCollection services,
        Type interfaceType,
        Type implementationType)
    {
        return services.AddGeneric(interfaceType, implementationType, ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddGenericDecorator(
        this IServiceCollection services,
        Type interfaceType,
        Type decoratorType,
        ServiceLifetime? lifetime = null)
    {
        if (!interfaceType.IsGenericTypeDefinition)
            throw new ArgumentException("Interface type must be a generic type definition.", interfaceType.Name);

        if (!decoratorType.IsGenericTypeDefinition)
            throw new ArgumentException("Decorator type must be a generic type definition.", decoratorType.Name);

        var descriptors = services
            .Where(d => d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition() == interfaceType)
            .ToList();

        if (descriptors.Count == 0) {
            throw new ArgumentException(
                "The interface does not have an existing closed-type implementation registered. Register concrete implementations before adding a decorator.",
                interfaceType.Name);
        }

        foreach (var descriptor in descriptors) {
            var effectiveLifetime = lifetime ?? descriptor.Lifetime;
            var index = services.IndexOf(descriptor);

            services[index] = ServiceDescriptor.Describe(
                descriptor.ServiceType,
                sp =>
                {
                    var typeArgs = descriptor.ServiceType.GetGenericArguments();
                    var closedDecoratorType = decoratorType.MakeGenericType(typeArgs);

                    object inner;
                    if (descriptor.ImplementationType != null) {
                        inner = ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
                    }
                    else if (descriptor.ImplementationFactory != null) {
                        inner = descriptor.ImplementationFactory(sp);
                    }
                    else {
                        inner = descriptor.ImplementationInstance!;
                    }

                    return ActivatorUtilities.CreateInstance(sp, closedDecoratorType, inner);
                },
                effectiveLifetime);
        }

        return services;
    }
}