using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.Policies;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Composition;

public class PolicyListenerBuilderTests
{
    [Fact]
    public void UsingPolicyListener_WhenPolicyDispatcherNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton((object?)null, Substitute.For<IMessageReceiver>());
        var builder = new PolicyListenerBuilder(services);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IPolicyDispatcher*");
    }

    [Fact]
    public void UsingPolicyListener_WhenMessageReceiverNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();
        var builder = new PolicyListenerBuilder(services);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IMessageReceiver*");
    }

    [Fact]
    public void UsingPolicyListener_WhenAllDependenciesPresent_RegistersPolicyListenerAsHostedService()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();
        services.AddKeyedSingleton((object?)null, Substitute.For<IMessageReceiver>());
        var builder = new PolicyListenerBuilder(services);

        builder.Build();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == null);
    }

    [Fact]
    public void AddPolicyListener_WhenCalledTwiceWithDifferentNames_RegistersTwoHostedServices()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IPolicyDispatcher>("orders", Substitute.For<IPolicyDispatcher>());
        services.AddKeyedSingleton<IPolicyDispatcher>("payments", Substitute.For<IPolicyDispatcher>());
        services.AddKeyedSingleton("orders", Substitute.For<IMessageReceiver>());
        services.AddKeyedSingleton<IMessageReceiver>("payments", Substitute.For<IMessageReceiver>());
        var builder = new WriteModelBuilder(services);

        builder.AddPolicyListener("orders");
        builder.AddPolicyListener("payments");

        services.Where(s => s.ServiceType == typeof(IHostedService))
            .Should().HaveCount(2);
    }

    [Fact]
    public void AddPolicyListener_WhenReceiverKeyNotRegistered_ThrowsAtBuildTime()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IPolicyDispatcher>("unknown", Substitute.For<IPolicyDispatcher>());
        var builder = new PolicyListenerBuilder(services, "unknown");

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IMessageReceiver*'unknown'*");
    }

    [Fact]
    public void AddPolicyListener_WhenWithExceptionHandlerCalled_ResolvesSpecifiedType()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IPolicyDispatcher>("test", Substitute.For<IPolicyDispatcher>());
        services.AddKeyedSingleton("test", Substitute.For<IMessageReceiver>());
        services.AddSingleton<CustomTestExceptionHandler>();
        services.AddLogging();
        var builder = new PolicyListenerBuilder(services, "test");

        builder.WithExceptionHandler<CustomTestExceptionHandler>();
        builder.Build();

        var provider = services.BuildServiceProvider();
        var hostedService = provider.GetRequiredService<IHostedService>();
        hostedService.Should().NotBeNull();
    }

    [Fact]
    public void AddPolicyListener_BackwardsCompatibility_StillRegistersOneHostedService()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();
        services.AddKeyedSingleton((object?)null, Substitute.For<IMessageReceiver>());
        var builder = new WriteModelBuilder(services);

        builder.AddPolicyListener();

        services.Where(s => s.ServiceType == typeof(IHostedService))
            .Should().HaveCount(1);
    }
}

internal class CustomTestExceptionHandler : IMessageExceptionHandler
{
    public Task<MessageAction> HandleAsync(Message message, Exception exception, CancellationToken cancellationToken)
    {
        return Task.FromResult(MessageAction.Abandon);
    }
}