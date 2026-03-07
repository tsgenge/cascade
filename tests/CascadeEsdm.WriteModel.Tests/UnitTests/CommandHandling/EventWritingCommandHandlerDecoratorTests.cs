using AutoFixture;
using AutoFixture.Xunit2;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Exceptions;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class EventWritingCommandHandlerDecoratorTests
{
    private readonly IFixture _fixture;
    private readonly IEventStreamWriter _mockEventWriter;

    public EventWritingCommandHandlerDecoratorTests()
    {
        _fixture = new Fixture();
        _mockEventWriter = Substitute.For<IEventStreamWriter>();
    }

    public class GenericResponseDecoratorTests : EventWritingCommandHandlerDecoratorTests
    {
        [Fact]
        public void Constructor_WithNullInnerHandler_ThrowsArgumentNullException()
        {
            var act = () => new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                null!,
                _mockEventWriter);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("inner");
        }

        [Fact]
        public void Constructor_WithNullEventWriter_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();

            var act = () => new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("eventWriter");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();

            var act = () => new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task HandleAsync_CallsInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            await mockInner.Received(1).HandleAsync(commandEnvelope);
        }

        [Fact]
        public async Task HandleAsync_AddsAllEventsToEventWriter()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var events = new List<IEventEnvelope>
            {
                TestTools.CreateEventEnvelope(),
                TestTools.CreateEventEnvelope(),
                TestTools.CreateEventEnvelope()
            };
            var response = TestTools.CreateCommandResponse(commandEnvelope, events);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            _mockEventWriter.Received(3).Add(Arg.Any<IEventEnvelope>());
            _mockEventWriter.Received(1).Add(events[0]);
            _mockEventWriter.Received(1).Add(events[1]);
            _mockEventWriter.Received(1).Add(events[2]);
        }

        [Fact]
        public async Task HandleAsync_CallsSaveAsyncOnEventWriter()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            await _mockEventWriter.Received(1).SaveAsync();
        }

        [Fact]
        public async Task HandleAsync_ReturnsResponseFromInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedResponse = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(expectedResponse);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().BeSameAs(expectedResponse);
        }

        [Fact]
        public async Task HandleAsync_WithNoEvents_DoesNotAddToEventWriter()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            _mockEventWriter.DidNotReceive().Add(Arg.Any<IEventEnvelope>());
            await _mockEventWriter.Received(1).SaveAsync();
        }

        [Fact]
        public async Task HandleAsync_WhenEventWritingExceptionThrown_WrapsInCommandProcessingException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);
            var innerException = new InvalidOperationException("Database error");
            var eventWritingException = new EventWritingException(innerException);

            mockInner.HandleAsync(commandEnvelope).Returns(response);
            _mockEventWriter.SaveAsync().Throws(eventWritingException);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            await act.Should().ThrowAsync<CommandProcessingException>()
                .WithInnerException(typeof(EventWritingException));
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_PropagatesException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Inner handler error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Inner handler error");
            _mockEventWriter.DidNotReceive().Add(Arg.Any<IEventEnvelope>());
            await _mockEventWriter.DidNotReceive().SaveAsync();
        }

        [Fact]
        public async Task HandleAsync_SavesEventsInCorrectOrder()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var events = new List<IEventEnvelope>
            {
                TestTools.CreateEventEnvelope(),
                TestTools.CreateEventEnvelope()
            };
            var response = TestTools.CreateCommandResponse(commandEnvelope, events);
            var callOrder = new List<string>();

            mockInner.HandleAsync(commandEnvelope).Returns(response);
            _mockEventWriter.When(x => x.Add(Arg.Any<IEventEnvelope>()))
                .Do(_ => callOrder.Add("Add"));
            _mockEventWriter.When(x => x.SaveAsync())
                .Do(_ => callOrder.Add("Save"));

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            callOrder.Should().Equal("Add", "Add", "Save");
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid commandId)
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope(commandId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockEventWriter);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().NotBeNull();
            commandId.Should().NotBeEmpty();
        }
    }

    public class NonGenericResponseDecoratorTests : EventWritingCommandHandlerDecoratorTests
    {
        [Fact]
        public void Constructor_WithNullInnerHandler_ThrowsArgumentNullException()
        {
            var act = () => new EventWritingCommandHandlerDecorator<TestCommand>(
                null!,
                _mockEventWriter);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("inner");
        }

        [Fact]
        public void Constructor_WithNullEventWriter_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();

            var act = () => new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("eventWriter");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();

            var act = () => new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task HandleAsync_CallsInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            await mockInner.Received(1).HandleAsync(commandEnvelope);
        }

        [Fact]
        public async Task HandleAsync_AddsAllEventsToEventWriter()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var events = new List<IEventEnvelope>
            {
                TestTools.CreateEventEnvelope(),
                TestTools.CreateEventEnvelope(),
                TestTools.CreateEventEnvelope()
            };
            var response = TestTools.CreateCommandResponse(commandEnvelope, events);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            _mockEventWriter.Received(3).Add(Arg.Any<IEventEnvelope>());
            _mockEventWriter.Received(1).Add(events[0]);
            _mockEventWriter.Received(1).Add(events[1]);
            _mockEventWriter.Received(1).Add(events[2]);
        }

        [Fact]
        public async Task HandleAsync_CallsSaveAsyncOnEventWriter()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            await _mockEventWriter.Received(1).SaveAsync();
        }

        [Fact]
        public async Task HandleAsync_ReturnsResponseFromInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedResponse = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(expectedResponse);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().BeSameAs(expectedResponse);
        }

        [Fact]
        public async Task HandleAsync_WithNoEvents_DoesNotAddToEventWriter()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            _mockEventWriter.DidNotReceive().Add(Arg.Any<IEventEnvelope>());
            await _mockEventWriter.Received(1).SaveAsync();
        }

        [Fact]
        public async Task HandleAsync_WhenEventWritingExceptionThrown_WrapsInCommandProcessingException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);
            var innerException = new InvalidOperationException("Database error");
            var eventWritingException = new EventWritingException(innerException);

            mockInner.HandleAsync(commandEnvelope).Returns(response);
            _mockEventWriter.SaveAsync().Throws(eventWritingException);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            await act.Should().ThrowAsync<CommandProcessingException>()
                .WithInnerException(typeof(EventWritingException));
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrowsException_PropagatesException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedException = new InvalidOperationException("Inner handler error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Inner handler error");
            _mockEventWriter.DidNotReceive().Add(Arg.Any<IEventEnvelope>());
            await _mockEventWriter.DidNotReceive().SaveAsync();
        }

        [Fact]
        public async Task HandleAsync_SavesEventsInCorrectOrder()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var events = new List<IEventEnvelope>
            {
                TestTools.CreateEventEnvelope(),
                TestTools.CreateEventEnvelope()
            };
            var response = TestTools.CreateCommandResponse(commandEnvelope, events);
            var callOrder = new List<string>();

            mockInner.HandleAsync(commandEnvelope).Returns(response);
            _mockEventWriter.When(x => x.Add(Arg.Any<IEventEnvelope>()))
                .Do(_ => callOrder.Add("Add"));
            _mockEventWriter.When(x => x.SaveAsync())
                .Do(_ => callOrder.Add("Save"));

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            await decorator.HandleAsync(commandEnvelope);

            callOrder.Should().Equal("Add", "Add", "Save");
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid commandId)
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope(commandId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new EventWritingCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockEventWriter);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().NotBeNull();
            commandId.Should().NotBeEmpty();
        }
    }
}
