using AutoFixture;
using AutoFixture.Xunit2;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Exceptions;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Security;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.CommandHandling;

public class CommandHandlerTests
{
    private readonly IFixture _fixture;
    private readonly IAggregateHydrator<TestAggregate> _mockHydrator;
    private readonly ICommandAuthoriser _mockAuthoriser;
    private readonly ICommandExecutorFactory<TestAggregate> _mockExecutorFactory;
    private readonly ICommandExecutor<TestCommand, TestAggregate> _mockExecutor;

    public CommandHandlerTests()
    {
        _fixture = new Fixture();
        _mockHydrator = Substitute.For<IAggregateHydrator<TestAggregate>>();
        _mockAuthoriser = Substitute.For<ICommandAuthoriser>();
        _mockExecutorFactory = Substitute.For<ICommandExecutorFactory<TestAggregate>>();
        _mockExecutor = Substitute.For<ICommandExecutor<TestCommand, TestAggregate>>();
    }

    [Fact]
    public void Constructor_WithNullHydrator_ThrowsArgumentNullException()
    {
        var act = () => new TestCommandHandler(null!, _mockAuthoriser, _mockExecutorFactory);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("hydrator");
    }

    [Fact]
    public void Constructor_WithNullAuthoriser_ThrowsArgumentNullException()
    {
        var act = () => new TestCommandHandler(_mockHydrator, null!, _mockExecutorFactory);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("authoriser");
    }

    [Fact]
    public void Constructor_WithNullExecutorFactory_ThrowsArgumentNullException()
    {
        var act = () => new TestCommandHandler(_mockHydrator, _mockAuthoriser, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("executorFactory");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var act = () => new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task HandleAsync_HydratesAggregateWithCorrectSubjectId()
    {
        var subjectId = Guid.NewGuid();
        var commandEnvelope = TestTools.CreateCommandEnvelope(subjectId);
        var aggregate = new TestAggregate { Id = subjectId };
        
        _mockHydrator.HydrateAsync(subjectId, commandEnvelope.SecurityContext)
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(AsyncEnumerable.Empty<EventEnvelope>());

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        await handler.HandleAsync(commandEnvelope);

        await _mockHydrator.Received(1).HydrateAsync(subjectId, commandEnvelope.SecurityContext);
    }

    [Fact]
    public async Task HandleAsync_GetsExecutorFromFactory()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate();
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(AsyncEnumerable.Empty<EventEnvelope>());

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        await handler.HandleAsync(commandEnvelope);

        _mockExecutorFactory.Received(1).GetFor<TestCommand>();
    }

    [Fact]
    public async Task HandleAsync_ChecksAuthorisation()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate();
        var acl = Substitute.For<ISecurityDescriptor>();
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(acl);
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(AsyncEnumerable.Empty<EventEnvelope>());

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        await handler.HandleAsync(commandEnvelope);

        await _mockAuthoriser.Received(1).CanAsync(commandEnvelope, acl);
    }

    [Fact]
    public async Task HandleAsync_ExecutesCommandAndReturnsEvents()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate();
        var events = new List<EventEnvelope>
        {
            TestTools.CreateEventEnvelope(),
            TestTools.CreateEventEnvelope()
        };
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(events.ToAsyncEnumerable());

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        var result = await handler.HandleAsync(commandEnvelope);

        result.Should().NotBeNull();
        result.Events.Should().HaveCount(2);
        result.CommandId.Should().Be(commandEnvelope.Id);
    }

    [Fact]
    public async Task HandleAsync_WhenExecutorThrowsExceptionBase_RethrowsException()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate();
        var expectedException = new TestExceptionBase();
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Throws(expectedException);

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        var x = await Assert.ThrowsAsync<TestExceptionBase>(async () => await handler.HandleAsync(commandEnvelope));
        x.Should().BeSameAs(expectedException);
    }

    [Fact]
    public async Task HandleAsync_WhenExecutorThrowsGenericException_WrapsInCommandProcessingException()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate();
        var innerException = new InvalidOperationException("Test error");
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Throws(innerException);

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        var x = await Assert.ThrowsAsync<CommandProcessingException>(async () => await handler.HandleAsync(commandEnvelope));
        x.InnerException.Should().Be(innerException);
    }

    [Fact]
    public async Task HandleAsync_WithNoEvents_ReturnsResponseWithEmptyEventList()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate();
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(AsyncEnumerable.Empty<EventEnvelope>());

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        var result = await handler.HandleAsync(commandEnvelope);

        result.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_CallsCreateResponseWithCorrectParameters()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate { Id = Guid.NewGuid() };
        var events = new List<EventEnvelope> { TestTools.CreateEventEnvelope() };
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(events.ToAsyncEnumerable());

        var handler = new TestCommandHandlerWithTracking(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        var result = await handler.HandleAsync(commandEnvelope);

        handler.CreateResponseCalled.Should().BeTrue();
        handler.LastCommandEnvelope.Should().Be(commandEnvelope);
        handler.LastAggregate.Should().Be(aggregate);
        handler.LastEvents.Should().HaveCount(1);
    }

    [Theory]
    [AutoData]
    public async Task HandleAsync_WithAutoFixtureData_ExecutesSuccessfully(Guid subjectId)
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope(subjectId);
        var aggregate = new TestAggregate { Id = subjectId };
        
        _mockHydrator.HydrateAsync(subjectId, Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(AsyncEnumerable.Empty<EventEnvelope>());

        var handler = new TestCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        var result = await handler.HandleAsync(commandEnvelope);

        result.Should().NotBeNull();
        subjectId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BaseCommandHandler_CreatesDefaultCommandResponse()
    {
        var commandEnvelope = TestTools.CreateCommandEnvelope();
        var aggregate = new TestAggregate();
        var events = new List<EventEnvelope> { TestTools.CreateEventEnvelope() };
        
        _mockHydrator.HydrateAsync(Arg.Any<Guid>(), Arg.Any<IAuthenticatedContext>())
            .Returns(aggregate);
        _mockExecutorFactory.GetFor<TestCommand>().Returns(_mockExecutor);
        _mockExecutor.GetSecurityDescriptorAsync(commandEnvelope, aggregate)
            .Returns(Task.FromResult<ISecurityDescriptor?>(null));
        _mockExecutor.ExecuteAsync(commandEnvelope, aggregate)
            .Returns(events.ToAsyncEnumerable());

        var handler = new TestBaseCommandHandler(_mockHydrator, _mockAuthoriser, _mockExecutorFactory);

        var result = await handler.HandleAsync(commandEnvelope);

        result.Should().BeOfType<CommandResponse>();
        result.CommandId.Should().Be(commandEnvelope.Id);
        result.CommandType.Should().Be(commandEnvelope.Type);
        result.Events.Should().HaveCount(1);
    }

    private static async IAsyncEnumerable<T> ThrowAsync<T>(Exception exception)
        where T : IDomainEvent
    {
        yield return default(T);
        throw exception;
    }
}

internal class TestCommandHandler : CascadeEsdm.WriteModel.CommandHandling.CommandHandler<TestCommand, TestAggregate, CommandResponse>
{
    public TestCommandHandler(
        IAggregateHydrator<TestAggregate> hydrator,
        ICommandAuthoriser authoriser,
        ICommandExecutorFactory<TestAggregate> executorFactory)
        : base(hydrator, authoriser, executorFactory)
    {
    }

    protected override CommandResponse CreateResponse(
        ICommandEnvelope<TestCommand> commandEnvelope,
        TestAggregate aggregate,
        IReadOnlyList<IEventEnvelope> events)
    {
        return new CommandResponse(commandEnvelope, commandEnvelope.Command.GetSubject(commandEnvelope), events);
    }
}

internal class TestCommandHandlerWithTracking : CascadeEsdm.WriteModel.CommandHandling.CommandHandler<TestCommand, TestAggregate, CommandResponse>
{
    public bool CreateResponseCalled { get; private set; }
    public ICommandEnvelope<TestCommand>? LastCommandEnvelope { get; private set; }
    public TestAggregate? LastAggregate { get; private set; }
    public IReadOnlyList<IEventEnvelope>? LastEvents { get; private set; }

    public TestCommandHandlerWithTracking(
        IAggregateHydrator<TestAggregate> hydrator,
        ICommandAuthoriser authoriser,
        ICommandExecutorFactory<TestAggregate> executorFactory)
        : base(hydrator, authoriser, executorFactory)
    {
    }

    protected override CommandResponse CreateResponse(
        ICommandEnvelope<TestCommand> commandEnvelope,
        TestAggregate aggregate,
        IReadOnlyList<IEventEnvelope> events)
    {
        CreateResponseCalled = true;
        LastCommandEnvelope = commandEnvelope;
        LastAggregate = aggregate;
        LastEvents = events;
        return new CommandResponse(commandEnvelope, commandEnvelope.Command.GetSubject(commandEnvelope), events);
    }
}

internal class TestBaseCommandHandler : CascadeEsdm.WriteModel.CommandHandling.CommandHandler<TestCommand, TestAggregate>
{
    public TestBaseCommandHandler(
        IAggregateHydrator<TestAggregate> hydrator,
        ICommandAuthoriser authoriser,
        ICommandExecutorFactory<TestAggregate> executorFactory)
        : base(hydrator, authoriser, executorFactory)
    {
    }
}

public class TestExceptionBase : ExceptionBase
{
    public TestExceptionBase() : base("Test exception")
    {
    }
}
