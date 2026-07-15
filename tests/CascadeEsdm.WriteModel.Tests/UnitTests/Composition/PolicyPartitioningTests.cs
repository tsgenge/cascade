using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.TestDomain.People.Events;
using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.Policies;
using CascadeEsdm.WriteModel.Tests.UnitTests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using System.Text.Json;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Composition;

public class PolicyPartitioningTests
{
    [Fact]
    public void UsingPolicies_WhenUnkeyed_RegistersUnkeyedDispatcher()
    {
        var services = new ServiceCollection();
        var builder = new WriteModelBuilder(services);

        builder.UsingPolicies(p => p.AddPolicy<SharedRecordingPolicy>());

        services.Should().ContainSingle(s =>
            s.ServiceType == typeof(IPolicyDispatcher) && !s.IsKeyedService);
        services.Should().NotContain(s =>
            s.ServiceType == typeof(IPolicyDispatcher) && s.IsKeyedService);
    }

    [Fact]
    public void UsingPolicies_WhenKeyed_RegistersKeyedDispatcher()
    {
        var services = new ServiceCollection();
        var builder = new WriteModelBuilder(services);

        builder.UsingPolicies("orders", p => p.AddPolicy<OrdersRecordingPolicy>());

        services.Should().ContainSingle(s =>
            s.ServiceType == typeof(IPolicyDispatcher) &&
            s.IsKeyedService &&
            Equals(s.ServiceKey, "orders"));
        services.Should().NotContain(s =>
            s.ServiceType == typeof(IPolicyDispatcher) && !s.IsKeyedService);
    }

    [Fact]
    public async Task Dispatchers_WhenMultiplePartitionsRegistered_EachDispatchesOnlyItsOwnPolicies()
    {
        var recorder = new PolicyExecutionRecorder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);
        var builder = new WriteModelBuilder(services);

        builder.UsingPolicies(p => p.AddPolicy<SharedRecordingPolicy>());
        builder.UsingPolicies("orders", p => p.AddPolicy<OrdersRecordingPolicy>());
        builder.UsingPolicies("payments", p => p.AddPolicy<PaymentsRecordingPolicy>());

        var provider = services.BuildServiceProvider();
        var envelope = TestTools.CreateEventEnvelope();

        using (var scope = provider.CreateScope()) {
            await scope.ServiceProvider.GetRequiredService<IPolicyDispatcher>().DispatchAsync(envelope);
        }
        using (var scope = provider.CreateScope()) {
            await scope.ServiceProvider.GetRequiredKeyedService<IPolicyDispatcher>("orders").DispatchAsync(envelope);
        }
        using (var scope = provider.CreateScope()) {
            await scope.ServiceProvider.GetRequiredKeyedService<IPolicyDispatcher>("payments").DispatchAsync(envelope);
        }

        recorder.Executed.Should().BeEquivalentTo(
            nameof(SharedRecordingPolicy),
            nameof(OrdersRecordingPolicy),
            nameof(PaymentsRecordingPolicy));
    }

    [Fact]
    public void AddPolicyListener_WhenKeyedDispatcherMissing_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IMessageReceiver>("orders", Substitute.For<IMessageReceiver>());
        var builder = new WriteModelBuilder(services);

        var act = () => builder.AddPolicyListener("orders");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IPolicyDispatcher*'orders'*");
    }

    [Fact]
    public void AddPolicyListener_WhenKeyedDispatcherRegistered_Succeeds()
    {
        var recorder = new PolicyExecutionRecorder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);
        services.AddKeyedSingleton<IMessageReceiver>("orders", Substitute.For<IMessageReceiver>());
        var builder = new WriteModelBuilder(services);

        builder.UsingPolicies("orders", p => p.AddPolicy<OrdersRecordingPolicy>());
        var act = () => builder.AddPolicyListener("orders");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task KeyedListener_WhenMessageReceived_ExecutesOnlyItsPartitionPolicies()
    {
        var recorder = new PolicyExecutionRecorder();
        var sharedReceiver = new CapturingMessageReceiver();
        var ordersReceiver = new CapturingMessageReceiver();
        var services = BuildPartitionedServices(recorder, sharedReceiver, ordersReceiver);
        var provider = services.BuildServiceProvider();

        await StartAllHostedServicesAsync(provider);
        await ordersReceiver.EmitAsync(CreateEnvelopeMessage());

        recorder.Executed.Should().ContainSingle()
            .Which.Should().Be(nameof(OrdersRecordingPolicy));
    }

    [Fact]
    public async Task UnkeyedListener_WhenMessageReceived_ExecutesOnlySharedPolicies()
    {
        var recorder = new PolicyExecutionRecorder();
        var sharedReceiver = new CapturingMessageReceiver();
        var ordersReceiver = new CapturingMessageReceiver();
        var services = BuildPartitionedServices(recorder, sharedReceiver, ordersReceiver);
        var provider = services.BuildServiceProvider();

        await StartAllHostedServicesAsync(provider);
        await sharedReceiver.EmitAsync(CreateEnvelopeMessage());

        recorder.Executed.Should().ContainSingle()
            .Which.Should().Be(nameof(SharedRecordingPolicy));
    }

    private static ServiceCollection BuildPartitionedServices(
        PolicyExecutionRecorder recorder,
        IMessageReceiver sharedReceiver,
        IMessageReceiver ordersReceiver)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(recorder);
        services.AddSingleton<ITelemetryLogger, NoopTelemetryLogger>();
        services.AddSingleton<IMessageReceiver>(sharedReceiver);
        services.AddKeyedSingleton<IMessageReceiver>("orders", ordersReceiver);

        var builder = new WriteModelBuilder(services);
        builder.UsingPolicies(p => p.AddPolicy<SharedRecordingPolicy>());
        builder.UsingPolicies("orders", p => p.AddPolicy<OrdersRecordingPolicy>());
        builder.AddPolicyListener(configure: l => l.WithSerialisationSettings(SerialisationSettings));
        builder.AddPolicyListener("orders", l => l.WithSerialisationSettings(SerialisationSettings));
        return services;
    }

    private static async Task StartAllHostedServicesAsync(IServiceProvider provider)
    {
        foreach (var hostedService in provider.GetServices<IHostedService>())
            await hostedService.StartAsync(CancellationToken.None);
    }

    private static readonly JsonSerializerOptions SerialisationSettings =
        DefaultSerialisationSettings.UsingTypeQualifiedName();

    private static Message CreateEnvelopeMessage()
    {
        var envelope = TestTools.CreateEventEnvelope(
            new PersonAdded(Guid.NewGuid(), "John", "Doe", "0400000000"));
        var body = JsonSerializer.Serialize(envelope, SerialisationSettings);
        return new Message(body, new Dictionary<string, object>());
    }
}

internal class CapturingMessageReceiver : IMessageReceiver
{
    private Func<Message, CancellationToken, Task>? _handler;

    public Task StartAsync(Func<Message, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        _handler = handler;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ApplyActionAsync(Message message, MessageAction action, Exception? ex, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EmitAsync(Message message)
    {
        if (_handler is null) throw new InvalidOperationException("Receiver was not started.");
        return _handler(message, CancellationToken.None);
    }
}

internal class PolicyExecutionRecorder
{
    private readonly List<string> _executed = new();
    public IReadOnlyList<string> Executed
    {
        get { lock (_executed) return _executed.ToList(); }
    }

    public void Record(string policyName)
    {
        lock (_executed) _executed.Add(policyName);
    }
}

internal abstract class RecordingPolicy : IPolicy
{
    private readonly PolicyExecutionRecorder _recorder;
    protected RecordingPolicy(PolicyExecutionRecorder recorder) => _recorder = recorder;

    public bool Supports(EventEnvelope envelope) => true;

    public Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _recorder.Record(GetType().Name);
        return Task.CompletedTask;
    }
}

internal class SharedRecordingPolicy : RecordingPolicy
{
    public SharedRecordingPolicy(PolicyExecutionRecorder recorder) : base(recorder) { }
}

internal class OrdersRecordingPolicy : RecordingPolicy
{
    public OrdersRecordingPolicy(PolicyExecutionRecorder recorder) : base(recorder) { }
}

internal class PaymentsRecordingPolicy : RecordingPolicy
{
    public PaymentsRecordingPolicy(PolicyExecutionRecorder recorder) : base(recorder) { }
}
