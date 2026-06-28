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
    public void WithPolicyListener_WhenPolicyDispatcherNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IMessageReceiver>());
        var builder = new PolicyListenerBuilder(services);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IPolicyDispatcher*");
    }

    [Fact]
    public void WithPolicyListener_WhenMessageReceiverNotRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();
        var builder = new PolicyListenerBuilder(services);

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IMessageReceiver*");
    }

    [Fact]
    public void WithPolicyListener_WhenAllDependenciesPresent_RegistersPolicyListenerAsHostedService()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();
        services.AddSingleton(Substitute.For<IMessageReceiver>());
        var builder = new PolicyListenerBuilder(services);

        builder.Build();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(PolicyListener));
    }

    [Fact]
    public void WithPolicyListener_WhenNoExceptionHandlerRegistered_RegistersDefaultMessageExceptionHandler()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();
        services.AddSingleton(Substitute.For<IMessageReceiver>());
        var builder = new PolicyListenerBuilder(services);

        builder.Build();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IMessageExceptionHandler) &&
            s.ImplementationType == typeof(DefaultMessageExceptionHandler));
    }

    [Fact]
    public void WithPolicyListener_WhenCustomExceptionHandlerRegistered_DoesNotOverrideIt()
    {
        var services = new ServiceCollection();
        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();
        services.AddSingleton(Substitute.For<IMessageReceiver>());
        services.AddSingleton<IMessageExceptionHandler, CustomTestExceptionHandler>();
        var builder = new PolicyListenerBuilder(services);

        builder.Build();

        services.Where(s => s.ServiceType == typeof(IMessageExceptionHandler))
            .Should().HaveCount(1)
            .And.Contain(s => s.ImplementationType == typeof(CustomTestExceptionHandler));
    }
}

internal class CustomTestExceptionHandler : IMessageExceptionHandler
{
    public Task<MessageAction> HandleAsync(Message message, Exception exception, CancellationToken cancellationToken)
    {
        return Task.FromResult(MessageAction.Abandon);
    }
}
