using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Policies;
using CascadeEsdm.WriteModel.Tests.UnitTests;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Policies;

public class PolicyDispatcherTests
{
    private readonly IPolicy _mockPolicy;
    private readonly IPolicy _mockPolicy2;
    private readonly ILogger<PolicyDispatcher> _logger = NullLogger<PolicyDispatcher>.Instance;

    public PolicyDispatcherTests()
    {
        _mockPolicy = Substitute.For<IPolicy>();
        _mockPolicy2 = Substitute.For<IPolicy>();
    }

    [Fact]
    public void Constructor_WithNullPolicies_ThrowsArgumentNullException()
    {
        var act = () => new PolicyDispatcher(null!, _logger);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policies");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new PolicyDispatcher(Array.Empty<IPolicy>(), null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var policies = Array.Empty<IPolicy>();

        var act = () => new PolicyDispatcher(policies, _logger);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task DispatchAsync_WithNullEnvelope_ThrowsArgumentNullException()
    {
        var dispatcher = new PolicyDispatcher(Array.Empty<IPolicy>(), _logger);

        var act = () => dispatcher.DispatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("envelope");
    }

    [Fact]
    public async Task DispatchAsync_WithNoSupportingPolicies_CompletesWithoutError()
    {
        _mockPolicy.Supports(Arg.Any<EventEnvelope>()).Returns(false);
        var dispatcher = new PolicyDispatcher([_mockPolicy], _logger);
        var envelope = TestTools.CreateEventEnvelope();

        await dispatcher.DispatchAsync(envelope);

        await _mockPolicy.DidNotReceive().ExecuteAsync(Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithNoPoliciesRegistered_CompletesWithoutError()
    {
        var dispatcher = new PolicyDispatcher(Array.Empty<IPolicy>(), _logger);
        var envelope = TestTools.CreateEventEnvelope();

        await dispatcher.DispatchAsync(envelope);
    }

    [Fact]
    public async Task DispatchAsync_WithSingleSupportingPolicy_ExecutesPolicy()
    {
        var envelope = TestTools.CreateEventEnvelope();
        _mockPolicy.Supports(envelope).Returns(true);
        _mockPolicy.ExecuteAsync(envelope, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var dispatcher = new PolicyDispatcher([_mockPolicy], _logger);

        await dispatcher.DispatchAsync(envelope);

        await _mockPolicy.Received(1).ExecuteAsync(envelope, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleSupportingPolicies_ExecutesAllPolicies()
    {
        var envelope = TestTools.CreateEventEnvelope();
        _mockPolicy.Supports(envelope).Returns(true);
        _mockPolicy2.Supports(envelope).Returns(true);
        _mockPolicy.ExecuteAsync(envelope, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _mockPolicy2.ExecuteAsync(envelope, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var dispatcher = new PolicyDispatcher([_mockPolicy, _mockPolicy2], _logger);

        await dispatcher.DispatchAsync(envelope);

        await _mockPolicy.Received(1).ExecuteAsync(envelope, Arg.Any<CancellationToken>());
        await _mockPolicy2.Received(1).ExecuteAsync(envelope, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithMixedSupportingPolicies_ExecutesOnlySupporting()
    {
        var envelope = TestTools.CreateEventEnvelope();
        _mockPolicy.Supports(envelope).Returns(true);
        _mockPolicy2.Supports(envelope).Returns(false);
        _mockPolicy.ExecuteAsync(envelope, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var dispatcher = new PolicyDispatcher([_mockPolicy, _mockPolicy2], _logger);

        await dispatcher.DispatchAsync(envelope);

        await _mockPolicy.Received(1).ExecuteAsync(envelope, Arg.Any<CancellationToken>());
        await _mockPolicy2.DidNotReceive().ExecuteAsync(Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WhenPolicyFails_ThrowsPolicyExecutionException()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var policyException = new InvalidOperationException("Policy failed");
        _mockPolicy.Supports(envelope).Returns(true);
        _mockPolicy.ExecuteAsync(envelope, Arg.Any<CancellationToken>())
            .ThrowsAsync(policyException);
        var dispatcher = new PolicyDispatcher([_mockPolicy], _logger);

        var ex = await dispatcher
            .Awaiting(d => d.DispatchAsync(envelope))
            .Should().ThrowAsync<PolicyExecutionException>();
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
        ex.Which.InnerExceptions.Should().HaveCount(1);
        ex.Which.Message.Should().StartWith($"One or more policies failed to execute: {_mockPolicy.GetType().Name}");
    }

    [Fact]
    public async Task DispatchAsync_WhenOnePolicyFailsAndAnotherSucceeds_SuccessfulPolicyCompletes()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var successfulPolicy = new TrackingTestPolicy();
        var failingPolicy = new FailingTestPolicy();
        var dispatcher = new PolicyDispatcher([successfulPolicy, failingPolicy], _logger);

        await dispatcher
            .Awaiting(d => d.DispatchAsync(envelope))
            .Should().ThrowAsync<PolicyExecutionException>();
        successfulPolicy.Executed.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WhenMultiplePoliciesFail_ExceptionContainsAllFailures()
    {
        var envelope = TestTools.CreateEventEnvelope();
        var failingPolicy1 = new FailingTestPolicy();
        var failingPolicy2 = new AnotherFailingTestPolicy();
        var dispatcher = new PolicyDispatcher([failingPolicy1, failingPolicy2], _logger);

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
        var policy1 = new TrackingTestPolicy();
        var policy2 = new TrackingTestPolicy();
        var dispatcher = new PolicyDispatcher([policy1, policy2], _logger);

        await dispatcher.DispatchAsync(envelope);

        policy1.Executed.Should().BeTrue();
        policy2.Executed.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_PassesCancellationTokenToPolicy()
    {
        var envelope = TestTools.CreateEventEnvelope();
        using var cts = new CancellationTokenSource();
        _mockPolicy.Supports(envelope).Returns(true);
        _mockPolicy.ExecuteAsync(envelope, cts.Token).Returns(Task.CompletedTask);
        var dispatcher = new PolicyDispatcher([_mockPolicy], _logger);

        await dispatcher.DispatchAsync(envelope, cts.Token);

        await _mockPolicy.Received(1).ExecuteAsync(envelope, cts.Token);
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
    public bool Executed { get; private set; }

    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}
