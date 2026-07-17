using AutoFixture;
using AutoFixture.Xunit2;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using CascadeEsdm.WriteModel.CommandHandling;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class LoggingCommandHandlerDecoratorTests
{
    private readonly IFixture _fixture;
    private readonly ITelemetryLogger _mockTelemetryLogger;
    private readonly ILogger<LoggingCommandHandlerDecorator<TestCommand>> _mockLogger;
    private readonly IDisposable _mockOperation;
    private readonly IDisposable _mockScope;

    public LoggingCommandHandlerDecoratorTests()
    {
        _fixture = new Fixture();
        _mockTelemetryLogger = Substitute.For<ITelemetryLogger>();
        _mockLogger = Substitute.For<ILogger<LoggingCommandHandlerDecorator<TestCommand>>>();
        _mockOperation = Substitute.For<IDisposable>();
        _mockScope = Substitute.For<IDisposable>();

        _mockTelemetryLogger.StartOperation(Arg.Any<string>(), Arg.Any<TelemetryParent>(), Arg.Any<TelemetryOperationKind>())
            .Returns(_mockOperation);
        _mockLogger.BeginScope(Arg.Any<object>())
            .Returns(_mockScope);
    }

    public class ExplicitCommandResponseTests : LoggingCommandHandlerDecoratorTests
    {
        [Fact]
        public void Constructor_WithNullInnerHandler_ThrowsArgumentNullException()
        {
            var act = () => new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                null!,
                _mockTelemetryLogger,
                _mockLogger);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("inner");
        }

        [Fact]
        public void Constructor_WithNullTelemetryLogger_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();

            var act = () => new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                null!,
                _mockLogger);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("telemetryLogger");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();

            var act = () => new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();

            var act = () => new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task HandleAsync_CallsInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            await mockInner.Received(1).HandleAsync(commandEnvelope);
        }

        [Fact]
        public async Task HandleAsync_StartsOperationWithCorrectName()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockTelemetryLogger.Received(1).StartOperation(
                $"Executing {typeof(TestCommand).Name}",
                Arg.Any<TelemetryParent>(),
                Arg.Any<TelemetryOperationKind>());
        }

        [Fact]
        public async Task HandleAsync_BeginsLoggerScope()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockLogger.Received(1).BeginScope(Arg.Any<object>());
        }

        [Fact]
        public async Task HandleAsync_DisposesOperationAfterCompletion()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockOperation.Received(1).Dispose();
        }

        [Fact]
        public async Task HandleAsync_DisposesScopeAfterCompletion()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockScope.Received(1).Dispose();
        }

        [Fact]
        public async Task HandleAsync_ReturnsResponseFromInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedResponse = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(expectedResponse);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().BeSameAs(expectedResponse);
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_LogsError()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await decorator.HandleAsync(commandEnvelope));

            _mockLogger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                expectedException,
                Arg.Any<Func<object, Exception?, string>>());
            _mockTelemetryLogger.Received(1).TrackException(expectedException);
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_RethrowsException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Should().BeSameAs(expectedException);
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_DisposesOperationAndScope()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await decorator.HandleAsync(commandEnvelope));

            _mockOperation.Received(1).Dispose();
            _mockScope.Received(1).Dispose();
        }

        [Fact]
        public async Task HandleAsync_ExecutesInCorrectOrder()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);
            var callOrder = new List<string>();

            mockInner.HandleAsync(commandEnvelope).Returns(response);
            _mockTelemetryLogger.When(x => x.StartOperation(Arg.Any<string>(), Arg.Any<TelemetryParent>(), Arg.Any<TelemetryOperationKind>()))
                .Do(_ => callOrder.Add("StartOperation"));
            _mockLogger.When(x => x.BeginScope(Arg.Any<object>()))
                .Do(_ => callOrder.Add("BeginScope"));
            mockInner.When(x => x.HandleAsync(commandEnvelope))
                .Do(_ => callOrder.Add("HandleAsync"));

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            callOrder.Should().ContainInOrder("StartOperation", "BeginScope", "HandleAsync");
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid commandId)
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope(commandId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().NotBeNull();
            commandId.Should().NotBeEmpty();
        }
    }

    public class ImplicitCommandResponseTests : LoggingCommandHandlerDecoratorTests
    {
        [Fact]
        public void Constructor_WithNullInnerHandler_ThrowsArgumentNullException()
        {
            var act = () => new LoggingCommandHandlerDecorator<TestCommand>(
                null!,
                _mockTelemetryLogger,
                _mockLogger);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("inner");
        }

        [Fact]
        public void Constructor_WithNullTelemetryLogger_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();

            var act = () => new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                null!,
                _mockLogger);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("telemetryLogger");
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();

            var act = () => new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();

            var act = () => new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task HandleAsync_CallsInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            await mockInner.Received(1).HandleAsync(commandEnvelope);
        }

        [Fact]
        public async Task HandleAsync_StartsOperationWithCorrectName()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockTelemetryLogger.Received(1).StartOperation(
                $"Executing {typeof(TestCommand).Name}",
                Arg.Any<TelemetryParent>(),
                Arg.Any<TelemetryOperationKind>());
        }

        [Fact]
        public async Task HandleAsync_BeginsLoggerScope()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockLogger.Received(1).BeginScope(Arg.Any<object>());
        }

        [Fact]
        public async Task HandleAsync_DisposesOperationAfterCompletion()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockOperation.Received(1).Dispose();
        }

        [Fact]
        public async Task HandleAsync_DisposesScopeAfterCompletion()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            _mockScope.Received(1).Dispose();
        }

        [Fact]
        public async Task HandleAsync_ReturnsResponseFromInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedResponse = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(expectedResponse);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().BeSameAs(expectedResponse);
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_LogsError()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await decorator.HandleAsync(commandEnvelope));

            _mockLogger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                expectedException,
                Arg.Any<Func<object, Exception?, string>>());
            _mockTelemetryLogger.Received(1).TrackException(expectedException);
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_RethrowsException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            var exception = await act.Should().ThrowAsync<InvalidOperationException>();
            exception.Which.Should().BeSameAs(expectedException);
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_DisposesOperationAndScope()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await decorator.HandleAsync(commandEnvelope));

            _mockOperation.Received(1).Dispose();
            _mockScope.Received(1).Dispose();
        }

        [Fact]
        public async Task HandleAsync_ExecutesInCorrectOrder()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);
            var callOrder = new List<string>();

            mockInner.HandleAsync(commandEnvelope).Returns(response);
            _mockTelemetryLogger.When(x => x.StartOperation(Arg.Any<string>(), Arg.Any<TelemetryParent>(), Arg.Any<TelemetryOperationKind>()))
                .Do(_ => callOrder.Add("StartOperation"));
            _mockLogger.When(x => x.BeginScope(Arg.Any<object>()))
                .Do(_ => callOrder.Add("BeginScope"));
            mockInner.When(x => x.HandleAsync(commandEnvelope))
                .Do(_ => callOrder.Add("HandleAsync"));

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            await decorator.HandleAsync(commandEnvelope);

            callOrder.Should().ContainInOrder("StartOperation", "BeginScope", "HandleAsync");
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid commandId)
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope(commandId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new LoggingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockTelemetryLogger,
                _mockLogger);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().NotBeNull();
            commandId.Should().NotBeEmpty();
        }
    }
}