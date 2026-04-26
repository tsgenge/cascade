using CascadeEsdm.SharedKernel.Composition;
using Microsoft.Extensions.DependencyInjection;

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
            throw new ArgumentException("Implementation type must be a generic type definition.", nameof(implementationType));
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
        return services.AddGeneric(interfaceType, implementationType, ServiceLifetime.Scoped);
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
        if (!interfaceType.IsGenericTypeDefinition) {
            throw new ArgumentException("Interface type must be a generic type definition.", nameof(interfaceType));
        }

        if (!decoratorType.IsGenericTypeDefinition) {
            throw new ArgumentException("Decorator type must be a generic type definition.", nameof(decoratorType));
        }

        var descriptors = services
            .Where(d => d.ServiceType.IsGenericType && 
                       d.ServiceType.GetGenericTypeDefinition() == interfaceType)
            .ToList();

        if (descriptors.Count == 0) {
            var openDescriptors = services
                .Where(d => d.ServiceType == interfaceType)
                .ToList();

            foreach (var descriptor in openDescriptors) {
                var effectiveLifetime = lifetime ?? descriptor.Lifetime;
                var index = services.IndexOf(descriptor);
                
                services[index] = ServiceDescriptor.Describe(
                    interfaceType,
                    provider => {
                        var typeArgs = interfaceType.GetGenericArguments();
                        var closedInterfaceType = interfaceType.MakeGenericType(typeArgs);
                        var closedDecoratorType = decoratorType.MakeGenericType(typeArgs);
                        
                        object inner;
                        if (descriptor.ImplementationType != null) {
                            var closedImplementationType = descriptor.ImplementationType.IsGenericTypeDefinition
                                ? descriptor.ImplementationType.MakeGenericType(typeArgs)
                                : descriptor.ImplementationType;
                            inner = ActivatorUtilities.CreateInstance(provider, closedImplementationType);
                        }
                        else if (descriptor.ImplementationFactory != null) {
                            inner = descriptor.ImplementationFactory(provider);
                        }
                        else {
                            inner = descriptor.ImplementationInstance!;
                        }
                        
                        return ActivatorUtilities.CreateInstance(provider, closedDecoratorType, inner);
                    },
                    effectiveLifetime);
            }
        }
        else {
            foreach (var descriptor in descriptors) {
                var effectiveLifetime = lifetime ?? descriptor.Lifetime;
                var index = services.IndexOf(descriptor);
                
                services[index] = ServiceDescriptor.Describe(
                    descriptor.ServiceType,
                    provider => {
                        var typeArgs = descriptor.ServiceType.GetGenericArguments();
                        var closedDecoratorType = decoratorType.MakeGenericType(typeArgs);
                        
                        object inner;
                        if (descriptor.ImplementationType != null) {
                            inner = ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType);
                        }
                        else if (descriptor.ImplementationFactory != null) {
                            inner = descriptor.ImplementationFactory(provider);
                        }
                        else {
                            inner = descriptor.ImplementationInstance!;
                        }
                        
                        return ActivatorUtilities.CreateInstance(provider, closedDecoratorType, inner);
                    },
                    effectiveLifetime);
            }
        }

        return services;
    }

    public static IServiceCollection AddScopedGenericDecorator(
        this IServiceCollection services,
        Type interfaceType,
        Type decoratorType)
    {
        return services.AddGenericDecorator(interfaceType, decoratorType, ServiceLifetime.Scoped);
    }

    public static IServiceCollection AddTransientGenericDecorator(
        this IServiceCollection services,
        Type interfaceType,
        Type decoratorType)
    {
        return services.AddGenericDecorator(interfaceType, decoratorType, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddSingletonGenericDecorator(
        this IServiceCollection services,
        Type interfaceType,
        Type decoratorType)
    {
        return services.AddGenericDecorator(interfaceType, decoratorType, ServiceLifetime.Singleton);
    }
}
