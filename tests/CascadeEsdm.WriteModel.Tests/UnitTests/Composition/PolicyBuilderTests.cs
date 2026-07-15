using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.Policies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Composition;

public class PolicyBuilderTests
{
    [Fact]
    public void AddPolicy_WhenUnkeyed_RegistersUnkeyedScopedPolicy()
    {
        var services = new ServiceCollection();
        var builder = new PolicyBuilder(services);

        builder.AddPolicy<SharedTestPolicy>();

        services.Should().ContainSingle(s =>
            s.ServiceType == typeof(IPolicy) &&
            !s.IsKeyedService &&
            s.ImplementationType == typeof(SharedTestPolicy) &&
            s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPolicy_WhenKeyed_RegistersKeyedScopedPolicy()
    {
        var services = new ServiceCollection();
        var builder = new PolicyBuilder(services, "orders");

        builder.AddPolicy<OrdersTestPolicy>();

        services.Should().ContainSingle(s =>
            s.ServiceType == typeof(IPolicy) &&
            s.IsKeyedService &&
            Equals(s.ServiceKey, "orders") &&
            s.KeyedImplementationType == typeof(OrdersTestPolicy) &&
            s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddPolicy_WhenKeyed_DoesNotRegisterUnkeyedPolicy()
    {
        var services = new ServiceCollection();
        var builder = new PolicyBuilder(services, "orders");

        builder.AddPolicy<OrdersTestPolicy>();

        services.Should().NotContain(s =>
            s.ServiceType == typeof(IPolicy) && !s.IsKeyedService);
    }

    [Fact]
    public void GetKeyedServices_WhenPoliciesRegisteredUnderDifferentKeys_ReturnsOnlyMatchingPartition()
    {
        var services = new ServiceCollection();
        new PolicyBuilder(services).AddPolicy<SharedTestPolicy>();
        new PolicyBuilder(services, "orders").AddPolicy<OrdersTestPolicy>();
        new PolicyBuilder(services, "payments").AddPolicy<PaymentsTestPolicy>();
        var provider = services.BuildServiceProvider();

        provider.GetServices<IPolicy>().Should().ContainSingle()
            .Which.Should().BeOfType<SharedTestPolicy>();
        provider.GetKeyedServices<IPolicy>("orders").Should().ContainSingle()
            .Which.Should().BeOfType<OrdersTestPolicy>();
        provider.GetKeyedServices<IPolicy>("payments").Should().ContainSingle()
            .Which.Should().BeOfType<PaymentsTestPolicy>();
    }
}

internal class SharedTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope) => true;
    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal class OrdersTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope) => true;
    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal class PaymentsTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope) => true;
    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
