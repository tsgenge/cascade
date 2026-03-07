using AutoFixture;
using AutoFixture.Xunit2;
using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.WriteModel.CommandHandling;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class SerialisedCommandHandlerDecoratorTests
{
    private readonly IFixture _fixture;
    private readonly IDistributedLockProvider _mockLockProvider;
    private readonly IDistributedLock _mockLock;

    public SerialisedCommandHandlerDecoratorTests()
    {
        _fixture = new Fixture();
        _mockLockProvider = Substitute.For<IDistributedLockProvider>();
        _mockLock = Substitute.For<IDistributedLock>();

        _mockLockProvider.AcquireLockAsync(Arg.Any<string>())
            .Returns(_mockLock);
    }

    public class ExplicitCommandResponseTests : SerialisedCommandHandlerDecoratorTests
    {
        [Fact]
        public void Constructor_WithNullInnerHandler_ThrowsArgumentNullException()
        {
            var act = () => new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                null!,
                _mockLockProvider);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("inner");
        }

        [Fact]
        public void Constructor_WithNullLockProvider_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();

            var act = () => new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("lockProvider");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();

            var act = () => new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task HandleAsync_CallsInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await mockInner.Received(1).HandleAsync(commandEnvelope);
        }

        [Fact]
        public async Task HandleAsync_ReturnsResponseFromInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedResponse = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(expectedResponse);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().BeSameAs(expectedResponse);
        }

        [Fact]
        public async Task HandleAsync_WithCommandWithoutLockAttribute_DoesNotAcquireLock()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLockProvider.DidNotReceive().AcquireLockAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task HandleAsync_WithCommandLockAttribute_AcquiresLock()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand, CommandResponse>>();
            var commandEnvelope = CreateLockedCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLockProvider.Received(1).AcquireLockAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task HandleAsync_WithCommandLockLevelCommand_AcquiresLockWithCommandName()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand, CommandResponse>>();
            var subjectId = Guid.NewGuid();
            var commandEnvelope = CreateLockedCommandEnvelope(subjectId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);
            var subject = commandEnvelope.Command.GetSubject(commandEnvelope);
            var expectedLockName = $"{subject.ForStorage()}-{typeof(LockedTestCommand).Name}";

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLockProvider.Received(1).AcquireLockAsync(expectedLockName);
        }

        [Fact]
        public async Task HandleAsync_WithCommandLockLevelAggregate_AcquiresLockWithoutCommandName()
        {
            var mockInner = Substitute.For<ICommandHandler<AggregateLockedTestCommand, CommandResponse>>();
            var subjectId = Guid.NewGuid();
            var commandEnvelope = CreateAggregateLockedCommandEnvelope(subjectId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);
            var subject = commandEnvelope.Command.GetSubject(commandEnvelope);
            var expectedLockName = subject.ForStorage();

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<AggregateLockedTestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLockProvider.Received(1).AcquireLockAsync(expectedLockName);
        }

        [Fact]
        public async Task HandleAsync_DisposesLockAfterCompletion()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand, CommandResponse>>();
            var commandEnvelope = CreateLockedCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLock.Received(1).DisposeAsync();
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrows_DisposesLockAndRethrows()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand, CommandResponse>>();
            var commandEnvelope = CreateLockedCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Test error");
            await _mockLock.Received(1).DisposeAsync();
        }

        [Fact]
        public async Task HandleAsync_CalledMultipleTimesWithSameCommand_UsesCache()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand, CommandResponse>>();
            var commandEnvelope1 = CreateLockedCommandEnvelope();
            var commandEnvelope2 = CreateLockedCommandEnvelope();
            var response1 = TestTools.CreateCommandResponse(commandEnvelope1);
            var response2 = TestTools.CreateCommandResponse(commandEnvelope2);

            mockInner.HandleAsync(commandEnvelope1).Returns(response1);
            mockInner.HandleAsync(commandEnvelope2).Returns(response2);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope1);
            await decorator.HandleAsync(commandEnvelope2);

            await _mockLockProvider.Received(2).AcquireLockAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task HandleAsync_WithoutLockAttribute_DoesNotDisposeLock()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLock.DidNotReceive().DisposeAsync();
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid subjectId)
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand, CommandResponse>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope(subjectId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand, CommandResponse>(
                mockInner,
                _mockLockProvider);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().NotBeNull();
            subjectId.Should().NotBeEmpty();
        }
    }

    public class ImplicitCommandResponseTests : SerialisedCommandHandlerDecoratorTests
    {
        [Fact]
        public void Constructor_WithNullInnerHandler_ThrowsArgumentNullException()
        {
            var act = () => new SerialisedCommandHandlerDecorator<TestCommand>(
                null!,
                _mockLockProvider);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("inner");
        }

        [Fact]
        public void Constructor_WithNullLockProvider_ThrowsArgumentNullException()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();

            var act = () => new SerialisedCommandHandlerDecorator<TestCommand>(
                mockInner,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("lockProvider");
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();

            var act = () => new SerialisedCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockLockProvider);

            act.Should().NotThrow();
        }

        [Fact]
        public async Task HandleAsync_CallsInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await mockInner.Received(1).HandleAsync(commandEnvelope);
        }

        [Fact]
        public async Task HandleAsync_ReturnsResponseFromInnerHandler()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var expectedResponse = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(expectedResponse);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockLockProvider);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().BeSameAs(expectedResponse);
        }

        [Fact]
        public async Task HandleAsync_WithCommandWithoutLockAttribute_DoesNotAcquireLock()
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLockProvider.DidNotReceive().AcquireLockAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task HandleAsync_WithCommandLockAttribute_AcquiresLock()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand>>();
            var commandEnvelope = CreateLockedCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLockProvider.Received(1).AcquireLockAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task HandleAsync_DisposesLockAfterCompletion()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand>>();
            var commandEnvelope = CreateLockedCommandEnvelope();
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand>(
                mockInner,
                _mockLockProvider);

            await decorator.HandleAsync(commandEnvelope);

            await _mockLock.Received(1).DisposeAsync();
        }

        [Fact]
        public async Task HandleAsync_WhenInnerHandlerThrows_DisposesLockAndRethrows()
        {
            var mockInner = Substitute.For<ICommandHandler<LockedTestCommand>>();
            var commandEnvelope = CreateLockedCommandEnvelope();
            var expectedException = new InvalidOperationException("Test error");

            mockInner.HandleAsync(commandEnvelope).Throws(expectedException);

            var decorator = new SerialisedCommandHandlerDecorator<LockedTestCommand>(
                mockInner,
                _mockLockProvider);

            var act = async () => await decorator.HandleAsync(commandEnvelope);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Test error");
            await _mockLock.Received(1).DisposeAsync();
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid subjectId)
        {
            var mockInner = Substitute.For<ICommandHandler<TestCommand>>();
            var commandEnvelope = TestTools.CreateCommandEnvelope(subjectId);
            var response = TestTools.CreateCommandResponse(commandEnvelope);

            mockInner.HandleAsync(commandEnvelope).Returns(response);

            var decorator = new SerialisedCommandHandlerDecorator<TestCommand>(
                mockInner,
                _mockLockProvider);

            var result = await decorator.HandleAsync(commandEnvelope);

            result.Should().NotBeNull();
            subjectId.Should().NotBeEmpty();
        }
    }

    private static ICommandEnvelope<LockedTestCommand> CreateLockedCommandEnvelope(Guid? subjectId = null)
    {
        return new CommandEnvelope<LockedTestCommand>(
            new LockedTestCommand(subjectId ?? Guid.NewGuid()),
            new AuthenticatedContext(new(Guid.NewGuid()), new(Guid.NewGuid())),
            ClientChannel.Empty);
    }

    private static ICommandEnvelope<AggregateLockedTestCommand> CreateAggregateLockedCommandEnvelope(Guid? subjectId = null)
    {
        return new CommandEnvelope<AggregateLockedTestCommand>(
            new AggregateLockedTestCommand(subjectId ?? Guid.NewGuid()),
            new AuthenticatedContext(new(Guid.NewGuid()), new(Guid.NewGuid())),
            ClientChannel.Empty);
    }
}

[CommandLock(CommandLockLevel.Command)]
public record LockedTestCommand(Guid Id) : ICommand
{
    public ISubject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<TestAggregate>(Id);
    }
}

[CommandLock(CommandLockLevel.Aggregate)]
public record AggregateLockedTestCommand(Guid Id) : ICommand
{
    public ISubject GetSubject(ICommandEnvelope envelope)
    {
        return Subject.ForAggregate<TestAggregate>(Id);
    }
}
