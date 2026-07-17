using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using CascadeEsdm.SharedKernel.Infrastructure.Serialisation;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.TestDomain.People.Events;
using CascadeEsdm.WriteModel.Policies;
using CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Policies;

public class PolicyListenerTests
{
    private readonly IPolicyDispatcher _mockDispatcher;
    private readonly ITelemetryLogger _mockTelLogger;
    private readonly IMessageExceptionHandler _mockExceptionHandler;
    private readonly IMessageReceiver _mockReceiver;
    private readonly IServiceScopeFactory _mockScopeFactory;
    private readonly JsonSerializerOptions _serializerOptions;

    public PolicyListenerTests()
    {
        _mockDispatcher = Substitute.For<IPolicyDispatcher>();
        _mockTelLogger = Substitute.For<ITelemetryLogger>();

        var mockScope = Substitute.For<IServiceScope>();
        var mockScopeServiceProvider = Substitute.For<IServiceProvider>();
        mockScopeServiceProvider.GetService(typeof(IPolicyDispatcher)).Returns(_mockDispatcher);
        mockScopeServiceProvider.GetService(typeof(ITelemetryLogger)).Returns(_mockTelLogger);
        mockScope.ServiceProvider.Returns(mockScopeServiceProvider);

        _mockScopeFactory = Substitute.For<IServiceScopeFactory>();
        _mockScopeFactory.CreateScope().Returns(mockScope);

        _mockReceiver = Substitute.For<IMessageReceiver>();
        _mockExceptionHandler = Substitute.For<IMessageExceptionHandler>();
        _serializerOptions = DefaultSerialisationSettings.UsingTypeQualifiedName();
    }

    private PolicyListener CreateSut()
    {
        return new PolicyListener(_mockScopeFactory, _mockReceiver, _mockExceptionHandler,
            _serializerOptions, DispatcherKey.Default, NullLogger<PolicyListener>.Instance);
    }

    private async Task<Func<Message, CancellationToken, Task>> CaptureHandlerAsync()
    {
        Func<Message, CancellationToken, Task>? captured = null;
        _mockReceiver.StartAsync(Arg.Any<Func<Message, CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<Func<Message, CancellationToken, Task>>();
                return Task.CompletedTask;
            });

        var sut = CreateSut();
        await sut.StartAsync(CancellationToken.None);
        return captured!;
    }

    private Message CreateValidMessage()
    {
        var envelope = new EventEnvelope(
            EventSource.ForAggregate<TestAggregate>(Guid.NewGuid(), nameof(PersonAdded)),
            Subject.ForAggregate<TestAggregate>(Guid.NewGuid()),
            new AuthenticatedContext(new UserIdentity(Guid.NewGuid()), new Tenant(Guid.NewGuid())),
            ClientChannel.Empty,
            new PersonAdded(Guid.NewGuid(), "John", "Doe", "0400000000"),
            1);

        var body = JsonSerializer.Serialize(envelope, _serializerOptions);
        return new Message(body, new Dictionary<string, object>());
    }

    [Fact]
    public async Task PolicyListener_WhenMessageReceived_DeserialisesAndDispatchesToPolicyDispatcher()
    {
        var handler = await CaptureHandlerAsync();
        var message = CreateValidMessage();

        await handler(message, CancellationToken.None);

        _mockTelLogger.Received(1).StartOperation(Arg.Any<string>(), null, TelemetryOperationKind.Consumer);
        await _mockDispatcher.Received(1).DispatchAsync(Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PolicyListener_WhenDispatchSucceeds_CompletesMessage()
    {
        var handler = await CaptureHandlerAsync();
        var message = CreateValidMessage();

        await handler(message, CancellationToken.None);

        await _mockReceiver.Received(1)
            .ApplyActionAsync(message, MessageAction.Complete, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PolicyListener_WhenDispatchThrows_CallsExceptionHandler()
    {
        var handler = await CaptureHandlerAsync();
        var message = CreateValidMessage();
        var exception = new InvalidOperationException("dispatch failed");

        _mockDispatcher.DispatchAsync(Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
        _mockExceptionHandler.HandleAsync(Arg.Any<Message>(), Arg.Any<Exception>(), Arg.Any<CancellationToken>())
            .Returns(MessageAction.DeadLetter);

        await handler(message, CancellationToken.None);

        await _mockExceptionHandler.Received(1)
            .HandleAsync(message, Arg.Any<Exception>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PolicyListener_WhenDispatchThrows_TracksException()
    {
        var handler = await CaptureHandlerAsync();
        var message = CreateValidMessage();
        var exception = new InvalidOperationException("dispatch failed");

        _mockDispatcher.DispatchAsync(Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
        _mockExceptionHandler.HandleAsync(Arg.Any<Message>(), Arg.Any<Exception>(), Arg.Any<CancellationToken>())
            .Returns(MessageAction.DeadLetter);

        await handler(message, CancellationToken.None);

        _mockTelLogger.Received(1).TrackException(Arg.Is<Exception>(e => e.InnerException == exception));
    }

    [Fact]
    public async Task PolicyListener_WhenExceptionHandlerReturnsDeadLetter_DeadLettersMessage()
    {
        var handler = await CaptureHandlerAsync();
        var message = CreateValidMessage();

        var ex = new InvalidOperationException("fail");
        _mockDispatcher.DispatchAsync(Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ex);
        _mockExceptionHandler.HandleAsync(Arg.Any<Message>(), Arg.Any<Exception>(), Arg.Any<CancellationToken>())
            .Returns(MessageAction.DeadLetter);

        await handler(message, CancellationToken.None);

        await _mockReceiver.Received(1)
            .ApplyActionAsync(message, MessageAction.DeadLetter, Arg.Is<Exception>(e => e.InnerException == ex), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PolicyListener_WhenExceptionHandlerReturnsAbandon_AbandonsMessage()
    {
        var handler = await CaptureHandlerAsync();
        var message = CreateValidMessage();

        var ex = new InvalidOperationException("fail");
        _mockDispatcher.DispatchAsync(Arg.Any<EventEnvelope>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ex);
        _mockExceptionHandler.HandleAsync(Arg.Any<Message>(), Arg.Any<Exception>(), Arg.Any<CancellationToken>())
            .Returns(MessageAction.Abandon);

        await handler(message, CancellationToken.None);

        await _mockReceiver.Received(1)
            .ApplyActionAsync(message, MessageAction.Abandon, Arg.Is<Exception>(e => e.InnerException == ex), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PolicyListener_WhenCancelled_StopsReceiver()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();

        await sut.StopAsync(cts.Token);

        await _mockReceiver.Received(1).StopAsync(cts.Token);
    }
}