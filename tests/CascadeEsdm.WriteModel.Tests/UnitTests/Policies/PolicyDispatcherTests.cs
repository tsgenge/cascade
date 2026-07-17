using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Policies;
using CascadeEsdm.WriteModel.Tests.UnitTests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Policies;

public class PolicyDispatcherTests
{
    private static readonly ILogger<PolicyDispatcher> Logger = NullLogger<PolicyDispatcher>.Instance;

    public PolicyDispatcherTests()
    {
        TrackingTestPolicy.ExecutedInstances.Clear();
        AnotherTrackingTestPolicy.ExecutedInstances.Clear();
        NonSupportingTestPolicy.ExecutedInstances.Clear();
        ScopeRecordingPolicy.InstanceIds.Clear();
        AnotherScopeRecordingPolicy.InstanceIds.Clear();
    }

    private static IPolicyDispatcher CreateDispatcher(IEnumerable<Type> policyTypes, string? key = null)
    {
        var services = new ServiceCollection();
        foreach (var policyType in policyTypes) {
            services.AddScoped(policyType);
            services.AddSingleton(new PolicyRegister(key, policyType));
        }

        services.AddSingleton(Logger);

        if (key is null) {
            services.AddScoped<IPolicyDispatcher>(sp =>
                new PolicyDispatcher(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    null,
                    sp.GetServices<PolicyRegister>(),
                    sp.GetRequiredService<ILogger<PolicyDispatcher>>()));
        }
        else {
            services.AddKeyedScoped<IPolicyDispatcher>(key, (sp, serviceKey) =>
                new PolicyDispatcher(
                    sp.GetRequiredService<IServiceScopeFactory>(),
                    (string?)serviceKey,
                    sp.GetServices<PolicyRegister>(),
                    sp.GetRequiredService<ILogger<PolicyDispatcher>>()));
        }

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IPolicyDispatcher>();
    }

    [Fact]
    public void Constructor_WithNullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new PolicyDispatcher(null!, null, Array.Empty<PolicyRegister>(), Logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("scopeFactory");
    }

    [Fact]
    public void Constructor_WithNullPolicyRegisters_ThrowsArgumentNullException()
    {
        var act = () => new PolicyDispatcher(Substitute.For<IServiceScopeFactory>(), null, null!, Logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policyRegisters");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new PolicyDispatcher(Substitute.For<IServiceScopeFactory>(), null, Array.Empty<PolicyRegister>(), null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var act = () => new PolicyDispatcher(
            Substitute.For<IServiceScopeFactory>(),
            null,
            Array.Empty<PolicyRegister>(),
            Logger);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task DispatchAsync_WithNullEnvelope_ThrowsArgumentNullException()
    {
        var dispatcher = CreateDispatcher(Array.Empty<Type>());

        var act = () => dispatcher.DispatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("envelope");
    }

    [Fact]
    public async Task DispatchAsync_WithNoSupportingPolicies_CompletesWithoutError()
    {
        var dispatcher = CreateDispatcher([typeof(NonSupportingTestPolicy)]);
        var envelope = TestTools.CreateEventEnvelope();

        await dispatcher.DispatchAsync(envelope);
    }

    [Fact]
    public async Task DispatchAsync_WithNoPoliciesRegistered_CompletesWithoutError()
    {
        var dispatcher = CreateDispatcher(Array.Empty<Type>());
        var envelope = TestTools.CreateEventEnvelope();

        await dispatcher.DispatchAsync(envelope);
    }

    [Fact]
    public async Task DispatchAsync_WithSingleSupportingPolicy_ExecutesPolicy()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var dispatcher = CreateDispatcher([typeof(TrackingTestPolicy)]);

        await dispatcher.DispatchAsync(envelope);

        TrackingTestPolicy.ExecutedInstances.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleSupportingPolicies_ExecutesAllPolicies()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var dispatcher = CreateDispatcher([typeof(TrackingTestPolicy), typeof(AnotherTrackingTestPolicy)]);

        await dispatcher.DispatchAsync(envelope);

        TrackingTestPolicy.ExecutedInstances.Should().ContainSingle();
        AnotherTrackingTestPolicy.ExecutedInstances.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_WithMixedSupportingPolicies_ExecutesOnlySupporting()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var dispatcher = CreateDispatcher([typeof(TrackingTestPolicy), typeof(NonSupportingTestPolicy)]);

        await dispatcher.DispatchAsync(envelope);

        TrackingTestPolicy.ExecutedInstances.Should().ContainSingle();
        NonSupportingTestPolicy.ExecutedInstances.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_WhenPolicyFails_ThrowsPolicyExecutionException()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var dispatcher = CreateDispatcher([typeof(FailingTestPolicy)]);

        var ex = await dispatcher
            .Awaiting(d => d.DispatchAsync(envelope))
            .Should().ThrowAsync<PolicyExecutionException>();
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
        ex.Which.InnerExceptions.Should().HaveCount(1);
        ex.Which.Message.Should().StartWith($"One or more policies failed to execute: {nameof(FailingTestPolicy)}");
    }

    [Fact]
    public async Task DispatchAsync_WhenOnePolicyFailsAndAnotherSucceeds_SuccessfulPolicyCompletes()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var dispatcher = CreateDispatcher([typeof(TrackingTestPolicy), typeof(FailingTestPolicy)]);

        await dispatcher
            .Awaiting(d => d.DispatchAsync(envelope))
            .Should().ThrowAsync<PolicyExecutionException>();
        TrackingTestPolicy.ExecutedInstances.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_WhenMultiplePoliciesFail_ExceptionContainsAllFailures()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var dispatcher = CreateDispatcher([typeof(FailingTestPolicy), typeof(AnotherFailingTestPolicy)]);

        var exception = await dispatcher
            .Awaiting(d => d.DispatchAsync(envelope))
            .Should().ThrowAsync<PolicyExecutionException>();
        exception.Which.InnerExceptions.Should().HaveCount(2);
        exception.Which.Message.Should().StartWith($"One or more policies failed to execute: {nameof(FailingTestPolicy)}, {nameof(AnotherFailingTestPolicy)}");
    }

    [Fact]
    public async Task DispatchAsync_WhenAllPoliciesSucceed_DoesNotThrow()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var dispatcher = CreateDispatcher([typeof(TrackingTestPolicy), typeof(AnotherTrackingTestPolicy)]);

        await dispatcher.DispatchAsync(envelope);

        TrackingTestPolicy.ExecutedInstances.Should().ContainSingle();
        AnotherTrackingTestPolicy.ExecutedInstances.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchAsync_PassesCancellationTokenToPolicy()
    {
        var envelope = TestTools.CreateEventEnvelope();
        using var cts = new CancellationTokenSource();
        var dispatcher = CreateDispatcher([typeof(TokenCapturingTestPolicy)]);

        await dispatcher.DispatchAsync(envelope, cts.Token);

        TokenCapturingTestPolicy.LastReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task DispatchAsync_WithScopedPolicies_ExecutesEachInItsOwnScope()
    {
        var dispatcher = CreateDispatcher([typeof(ScopeRecordingPolicy), typeof(AnotherScopeRecordingPolicy)]);
        var envelope = TestTools.CreateEventEnvelope();

        await dispatcher.DispatchAsync(envelope);

        ScopeRecordingPolicy.InstanceIds.Should().HaveCount(1);
        AnotherScopeRecordingPolicy.InstanceIds.Should().HaveCount(1);
        ScopeRecordingPolicy.InstanceIds[0].Should().NotBe(AnotherScopeRecordingPolicy.InstanceIds[0]);
    }
}

internal class FailingTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Policy failed");
    }
}

internal class AnotherFailingTestPolicy : IPolicy
{
    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Another policy failed");
    }
}

internal class TrackingTestPolicy : IPolicy
{
    public static List<TrackingTestPolicy> ExecutedInstances { get; } = new();

    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ExecutedInstances.Add(this);
        return Task.CompletedTask;
    }
}

internal class AnotherTrackingTestPolicy : IPolicy
{
    public static List<AnotherTrackingTestPolicy> ExecutedInstances { get; } = new();

    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ExecutedInstances.Add(this);
        return Task.CompletedTask;
    }
}

internal class NonSupportingTestPolicy : IPolicy
{
    public static List<NonSupportingTestPolicy> ExecutedInstances { get; } = new();

    public bool Supports(EventEnvelope envelope) => false;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ExecutedInstances.Add(this);
        return Task.CompletedTask;
    }
}

internal class TokenCapturingTestPolicy : IPolicy
{
    public static CancellationToken LastReceivedToken { get; private set; }

    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        LastReceivedToken = cancellationToken;
        return Task.CompletedTask;
    }
}

internal class ScopeRecordingPolicy : IPolicy
{
    public static List<Guid> InstanceIds { get; } = new();

    private readonly Guid _instanceId = Guid.NewGuid();

    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        InstanceIds.Add(_instanceId);
        return Task.CompletedTask;
    }
}

internal class AnotherScopeRecordingPolicy : IPolicy
{
    public static List<Guid> InstanceIds { get; } = new();

    private readonly Guid _instanceId = Guid.NewGuid();

    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        InstanceIds.Add(_instanceId);
        return Task.CompletedTask;
    }
}
