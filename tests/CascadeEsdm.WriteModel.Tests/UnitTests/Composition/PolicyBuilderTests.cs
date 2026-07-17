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
            s.ServiceType == typeof(PolicyRegister) &&
            s.ImplementationInstance is PolicyRegister &&
            (s.ImplementationInstance as PolicyRegister)!.Key == null &&
            (s.ImplementationInstance as PolicyRegister)!.PolicyType == typeof(SharedTestPolicy) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddPolicy_WhenKeyed_RegistersKeyedScopedPolicy()
    {
        var services = new ServiceCollection();
        var builder = new PolicyBuilder(services, "orders");

        builder.AddPolicy<OrdersTestPolicy>();

        services.Should().ContainSingle(s =>
            s.ServiceType == typeof(PolicyRegister) &&
            s.ImplementationInstance is PolicyRegister &&
            (s.ImplementationInstance as PolicyRegister)!.Key == "orders" &&
            (s.ImplementationInstance as PolicyRegister)!.PolicyType == typeof(OrdersTestPolicy) &&
            s.Lifetime == ServiceLifetime.Singleton);
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

        var registers = provider.GetServices<PolicyRegister>().ToList();

        var defaults = registers.Where(r => r.Key is null).ToList();
        var orders = registers.Where(r => r.Key == "orders").ToList();
        var payments = registers.Where(r => r.Key == "payments").ToList();

        defaults.Should().NotBeNull();
        orders.Should().NotBeNull();
        payments.Should().NotBeNull();

        defaults.Should().HaveCount(1);
        defaults.First().PolicyType.Should().Be(typeof(SharedTestPolicy));
        orders.Should().HaveCount(1);
        orders.First().PolicyType.Should().Be(typeof(OrdersTestPolicy));
        payments.Should().HaveCount(1);
        payments.First().PolicyType.Should().Be(typeof(PaymentsTestPolicy));
    }
}

internal class SharedTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope)
    {
        return true;
    }

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal class OrdersTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope)
    {
        return true;
    }

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal class PaymentsTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope)
    {
        return true;
    }

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}