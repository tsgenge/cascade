using CascadeEsdm.ReadModel.Projecting;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace CascadeEsdm.ReadModel.UnitTests.Projecting;

public class ViewProjectorTests
{
    private readonly IEventCapabilityEvaluator<TestView> _evaluator = Substitute.For<IEventCapabilityEvaluator<TestView>>();
    private readonly IViewEventMapper<TestView> _mapper = Substitute.For<IViewEventMapper<TestView>>();
    private readonly IViewProjectionStore<TestView> _store = Substitute.For<IViewProjectionStore<TestView>>();
    private readonly IViewSequenceStore<TestView> _sequenceStore = Substitute.For<IViewSequenceStore<TestView>>();
    private readonly IAuthorResolver _authorResolver = Substitute.For<IAuthorResolver>();

    private ViewProjector<TestView> CreateSut(IAuthorResolver? authorResolver = null) =>
        new(_evaluator, _mapper, _store, _sequenceStore, authorResolver);

    private static Subject CreateSubject() => new(Guid.NewGuid(), "TestAggregate");

    private static EventEnvelope CreateEnvelope(Subject? subject = null, int sequence = 1) =>
        new(
            source: new EventSource("TestAssembly/TestAggregate", Guid.NewGuid(), "TestCommand"),
            subject: subject ?? CreateSubject(),
            securityContext: new AuthenticatedContext(
                new UserIdentity(Guid.NewGuid()),
                new Tenant(Guid.NewGuid())),
            channel: ClientChannel.Empty,
            @event: new TestEvent(),
            sequence: sequence);

    [Fact]
    public async Task ProjectAsync_WhenEventIsStale_ReturnsStale()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 3);

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(new Sequence(subject, DateTimeOffset.UtcNow, 5));

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.Stale);
        result.Effected.Should().BeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_WhenSequenceGapDetected_ReturnsReplay()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 5);

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(new Sequence(subject, DateTimeOffset.UtcNow, 2));

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.Replay);
        result.ReplayTime.Should().NotBeNull();
    }

    [Fact]
    public async Task ProjectAsync_WhenEventNotSupported_ReturnsNotApplicable_AndSavesSequence()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 1);

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(Sequence.Initial(subject));
        _evaluator.Supports(envelope).Returns(false);

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.NotApplicable);
        await _sequenceStore.Received(1).SaveAsync(Arg.Is<Sequence>(s => s.Value == 1));
    }

    [Fact]
    public async Task ProjectAsync_WhenAddsRow_CreatesNewView_AppliesEvent_AndReturnsSuccess()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 1);
        var partition = new Partition("test-partition");
        var newRowId = Guid.NewGuid();

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(Sequence.Initial(subject));
        _evaluator.Supports(envelope).Returns(true);
        _evaluator.AddsRow(envelope).Returns(true);
        _evaluator.RemovesRow(envelope).Returns(false);
        _store.GetRowsAsync(envelope).Returns((new List<TestView>(), partition));
        _mapper.GetNewRowId(envelope).Returns(newRowId);

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.Success);
        result.Effected.Should().HaveCount(1);
        result.Effected[0].Effect.Should().Be(ProjectionEffect.Added);
        result.Effected[0].View.Id.Should().Be(newRowId);
        result.Effected[0].Partition.Should().Be(partition);

        _mapper.Received(1).Map(Arg.Is<TestView>(v => v.Id == newRowId), envelope);
        await _store.Received(1).SaveAsync(Arg.Any<IEnumerable<TestView>>(), envelope);
        await _sequenceStore.Received(1).SaveAsync(Arg.Is<Sequence>(s => s.Value == 1));
    }

    [Fact]
    public async Task ProjectAsync_WhenAddsRow_SetsParentIdFromSubject()
    {
        var parentId = Guid.NewGuid();
        var subject = new Subject(Guid.NewGuid(), "TestAggregate", parentId);
        var envelope = CreateEnvelope(subject, sequence: 1);
        var partition = new Partition("test-partition");

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(Sequence.Initial(subject));
        _evaluator.Supports(envelope).Returns(true);
        _evaluator.AddsRow(envelope).Returns(true);
        _evaluator.RemovesRow(envelope).Returns(false);
        _store.GetRowsAsync(envelope).Returns((new List<TestView>(), partition));
        _mapper.GetNewRowId(envelope).Returns(Guid.NewGuid());

        var result = await CreateSut().ProjectAsync(envelope);

        result.Effected[0].View.ParentId.Should().Be(parentId);
    }

    [Fact]
    public async Task ProjectAsync_WhenChangesExistingRows_AppliesEvent_AndReturnsSuccess()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 1);
        var partition = new Partition("test-partition");
        var existingView = new TestView { Id = Guid.NewGuid() };

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(Sequence.Initial(subject));
        _evaluator.Supports(envelope).Returns(true);
        _evaluator.RemovesRow(envelope).Returns(false);
        _store.GetRowsAsync(envelope).Returns((new List<TestView> { existingView }, partition));

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.Success);
        result.Effected.Should().HaveCount(1);
        result.Effected[0].Effect.Should().Be(ProjectionEffect.Changed);
        result.Effected[0].View.Should().BeSameAs(existingView);

        _mapper.Received(1).Map(existingView, envelope);
        await _store.Received(1).SaveAsync(Arg.Any<IEnumerable<TestView>>(), envelope);
    }

    [Fact]
    public async Task ProjectAsync_WhenNoRowsFoundAndNotAdding_ReturnsRecordNotFound()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 1);
        var partition = new Partition("test-partition");

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(Sequence.Initial(subject));
        _evaluator.Supports(envelope).Returns(true);
        _evaluator.AddsRow(envelope).Returns(false);
        _evaluator.RemovesRow(envelope).Returns(false);
        _store.GetRowsAsync(envelope).Returns((new List<TestView>(), partition));

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.RecordNotFound);
        result.Effected.Should().BeEmpty();
    }

    [Fact]
    public async Task ProjectAsync_WhenRemovesRow_DeletesAndReturnsSuccess()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 1);
        var partition = new Partition("test-partition");
        var removedView = new TestView { Id = Guid.NewGuid() };
        var deletedProjections = new List<Projection<TestView>>
        {
            new(ProjectionEffect.Removed, removedView, partition)
        };

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(Sequence.Initial(subject));
        _evaluator.Supports(envelope).Returns(true);
        _evaluator.RemovesRow(envelope).Returns(true);
        _store.DeleteAsync(envelope).Returns(deletedProjections);

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.Success);
        result.Effected.Should().HaveCount(1);
        result.Effected[0].Effect.Should().Be(ProjectionEffect.Removed);

        await _store.DidNotReceive().GetRowsAsync(Arg.Any<EventEnvelope>());
        await _store.DidNotReceive().SaveAsync(Arg.Any<IEnumerable<TestView>>(), Arg.Any<EventEnvelope>());
    }

    [Fact]
    public async Task ProjectAsync_WhenAuthoredView_ResolvesAuthor()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 1);
        var partition = new Partition("test-partition");
        var authorId = new UserIdentity(Guid.NewGuid());

        var evaluator = Substitute.For<IEventCapabilityEvaluator<AuthoredTestView>>();
        var mapper = Substitute.For<IViewEventMapper<AuthoredTestView>>();
        var store = Substitute.For<IViewProjectionStore<AuthoredTestView>>();
        var seqStore = Substitute.For<IViewSequenceStore<AuthoredTestView>>();
        var resolver = Substitute.For<IAuthorResolver>();

        seqStore.GetLastSequenceAsync(subject).Returns(Sequence.Initial(subject));
        evaluator.Supports(envelope).Returns(true);
        evaluator.AddsRow(envelope).Returns(true);
        evaluator.RemovesRow(envelope).Returns(false);
        store.GetRowsAsync(envelope).Returns((new List<AuthoredTestView>(), partition));
        mapper.GetNewRowId(envelope).Returns(Guid.NewGuid());
        resolver.ResolveAsync(envelope.SecurityContext).Returns(authorId);

        var sut = new ViewProjector<AuthoredTestView>(evaluator, mapper, store, seqStore, resolver);

        var result = await sut.ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.Success);
        result.Effected[0].View.Author.Should().Be(authorId);
        await resolver.Received(1).ResolveAsync(envelope.SecurityContext);
    }

    [Fact]
    public async Task ProjectAsync_WhenSequenceIsExactlyNext_Processes()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 4);
        var partition = new Partition("test-partition");

        _sequenceStore.GetLastSequenceAsync(subject)
            .Returns(new Sequence(subject, DateTimeOffset.UtcNow, 3));
        _evaluator.Supports(envelope).Returns(true);
        _evaluator.RemovesRow(envelope).Returns(false);
        _evaluator.AddsRow(envelope).Returns(false);
        _store.GetRowsAsync(envelope).Returns((new List<TestView>(), partition));

        var result = await CreateSut().ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.RecordNotFound);
        await _sequenceStore.Received(1).SaveAsync(Arg.Is<Sequence>(s => s.Value == 4));
    }

    [Fact]
    public async Task ProjectAsync_WithoutAuthorResolver_SkipsAuthorResolution()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject, sequence: 1);
        var partition = new Partition("test-partition");

        var evaluator = Substitute.For<IEventCapabilityEvaluator<AuthoredTestView>>();
        var mapper = Substitute.For<IViewEventMapper<AuthoredTestView>>();
        var store = Substitute.For<IViewProjectionStore<AuthoredTestView>>();
        var seqStore = Substitute.For<IViewSequenceStore<AuthoredTestView>>();

        seqStore.GetLastSequenceAsync(subject).Returns(Sequence.Initial(subject));
        evaluator.Supports(envelope).Returns(true);
        evaluator.AddsRow(envelope).Returns(true);
        evaluator.RemovesRow(envelope).Returns(false);
        store.GetRowsAsync(envelope).Returns((new List<AuthoredTestView>(), partition));
        mapper.GetNewRowId(envelope).Returns(Guid.NewGuid());

        var sut = new ViewProjector<AuthoredTestView>(evaluator, mapper, store, seqStore);

        var result = await sut.ProjectAsync(envelope);

        result.Outcome.Should().Be(ProjectionOutcome.Success);
        result.Effected[0].View.Author.Should().BeNull();
    }
}

public class TestEvent : IDomainEvent;

public class TestView : IView
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public IList<string> ClientPermissions { get; set; } = new List<string>();
}

public class AuthoredTestView : IAuthoredView
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public IList<string> ClientPermissions { get; set; } = new List<string>();
    public UserIdentity Author { get; set; } = null!;
}
