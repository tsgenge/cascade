using AutoMapper;
using CascadeEsdm.ReadModel.Projecting;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.ReadModel.Querying;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using CascadeEsdm.SharedKernel.Security;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace CascadeEsdm.ReadModel.UnitTests.UnitTests.Projecting;

public class ViewProjectionStoreTests
{
    private readonly IPartitionedContainer<TestContainerDefinition> _container =
        Substitute.For<IPartitionedContainer<TestContainerDefinition>>();

    private readonly IMapper _mapper = Substitute.For<IMapper>();

    private readonly IProjectionPartitionLocator<TestView> _partitionLocator =
        Substitute.For<IProjectionPartitionLocator<TestView>>();

    private ViewProjectionStore<TestView, TestContainerDefinition> CreateSut()
    {
        return new ViewProjectionStore<TestView, TestContainerDefinition>(_container, _mapper, _partitionLocator);
    }

    private static Subject CreateSubject()
    {
        return new Subject(Guid.NewGuid(), "TestAggregate");
    }

    private static EventEnvelope CreateEnvelope(Subject? subject = null, int sequence = 1)
    {
        return new EventEnvelope(
            new EventSource("TestAssembly/TestAggregate", Guid.NewGuid(), "TestCommand"),
            subject ?? CreateSubject(),
            new AuthenticatedContext(
                new UserIdentity(Guid.NewGuid()),
                new Tenant(Guid.NewGuid())),
            ClientChannel.Empty,
            new TestEvent(),
            sequence);
    }

    private static Partition DefaultPartition()
    {
        return new Partition("test-partition");
    }

    private void SetupPartition(EventEnvelope envelope, Partition? partition = null)
    {
        _partitionLocator.GetPartition(envelope).Returns(partition ?? DefaultPartition());
    }

    private void SetupPageResult(params ViewDocument[] docs)
    {
        var result = new PageResult<ViewDocument>(
            docs.ToList(),
            new PageContinuationToken(null));

        _container.GetPageAsync<ViewDocument>(Arg.Any<PartitionedPageQuery>()).Returns(result);
    }

    private void SetupRowLocator(RowLocator<TestView>? locator)
    {
        if (locator != null) {
            _mapper.Map<RowLocator<TestView>>(Arg.Any<IDomainEvent>(),
                    Arg.Any<Action<IMappingOperationOptions<object, RowLocator<TestView>>>>())
                .Returns(locator);
        }
        else {
            _mapper.Map<RowLocator<TestView>>(Arg.Any<IDomainEvent>(),
                    Arg.Any<Action<IMappingOperationOptions<object, RowLocator<TestView>>>>())
                .Returns(x => throw new AutoMapperMappingException("No mapping"));
        }
    }

    // --- Constructor ---

    [Fact]
    public void Constructor_WhenStoreIsNull_ThrowsArgumentNullException()
    {
        var act = () => new ViewProjectionStore<TestView, TestContainerDefinition>(null!, _mapper, _partitionLocator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public void Constructor_WhenMapperIsNull_ThrowsArgumentNullException()
    {
        var act = () =>
            new ViewProjectionStore<TestView, TestContainerDefinition>(_container, null!, _partitionLocator);
        act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");
    }

    [Fact]
    public void Constructor_WhenPartitionLocatorIsNull_ThrowsArgumentNullException()
    {
        var act = () => new ViewProjectionStore<TestView, TestContainerDefinition>(_container, _mapper, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("partitionLocator");
    }

    // --- GetRowsAsync ---

    [Fact]
    public async Task GetRowsAsync_QueriesContainerWithCorrectPartition()
    {
        var envelope = CreateEnvelope();
        var partition = new Partition("my-partition");
        SetupPartition(envelope, partition);
        SetupRowLocator(null);
        SetupPageResult();

        await CreateSut().GetRowsAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q => q.PartitionKey == "my-partition"));
    }

    [Fact]
    public async Task GetRowsAsync_WhenNoRowLocator_QueriesBySubjectInSources()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject);
        SetupPartition(envelope);
        SetupRowLocator(null);
        SetupPageResult();

        await CreateSut().GetRowsAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q =>
                q.Query!.Contains("array_contains(c.sources, @subject)") &&
                q.QueryParameters["@subject"] == subject.Value &&
                q.QueryParameters["@type"] == nameof(TestView)));
    }

    [Fact]
    public async Task GetRowsAsync_WhenRowLocatorDefined_QueriesByLocatorProperty()
    {
        var envelope = CreateEnvelope();
        var locatorId = Guid.NewGuid();
        SetupPartition(envelope);
        SetupRowLocator(new RowLocator<TestView>(
            new KeyValuePair<string, Guid>("Id", locatorId),
            QueryOperation.EqualsValue));
        SetupPageResult();

        await CreateSut().GetRowsAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q =>
                q.Query!.Contains("c.view.id") &&
                q.QueryParameters["@selectValue"] == locatorId.ToString()));
    }

    [Fact]
    public async Task GetRowsAsync_ReturnsViewsFromDocuments()
    {
        var envelope = CreateEnvelope();
        var viewId = Guid.NewGuid();
        var view = new TestView { Id = viewId };
        SetupPartition(envelope);
        SetupRowLocator(null);
        SetupPageResult(new ViewDocument { Id = viewId, View = view, PartitionKey = "test-partition" });

        var (rows, _) = await CreateSut().GetRowsAsync(envelope);

        rows.Should().HaveCount(1);
        rows[0].Id.Should().Be(viewId);
    }

    [Fact]
    public async Task GetRowsAsync_ReturnsPartitionFromLocator()
    {
        var envelope = CreateEnvelope();
        var partition = new Partition("expected-partition");
        SetupPartition(envelope, partition);
        SetupRowLocator(null);
        SetupPageResult();

        var (_, returnedPartition) = await CreateSut().GetRowsAsync(envelope);

        returnedPartition.Should().Be(partition);
    }

    [Fact]
    public async Task GetRowsAsync_CachesDocumentsForSubsequentSave()
    {
        var envelope = CreateEnvelope();
        var viewId = Guid.NewGuid();
        var view = new TestView { Id = viewId };
        var partition = new Partition("p");

        SetupPartition(envelope, partition);
        SetupRowLocator(null);
        SetupPageResult(new ViewDocument
        {
            Id = viewId,
            PartitionKey = "p",
            View = view,
            Type = nameof(TestView),
            ETag = "cached-etag",
            Sources = new List<string> { envelope.Subject.Value }
        });

        var sut = CreateSut();
        await sut.GetRowsAsync(envelope);
        await sut.SaveAsync(new[] { view }, envelope);

        await _container.Received(1).UpsertBatchAsync(
            Arg.Is<IList<ViewDocument>>(docs =>
                docs[0].ETag == "cached-etag"));
    }

    [Fact]
    public async Task GetRowsAsync_ReturnsEmptyList_WhenNoDocumentsFound()
    {
        var envelope = CreateEnvelope();
        SetupPartition(envelope);
        SetupRowLocator(null);
        SetupPageResult();

        var (rows, _) = await CreateSut().GetRowsAsync(envelope);

        rows.Should().BeEmpty();
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_WhenNoRowLocator_ThrowsArgumentNullException()
    {
        var envelope = CreateEnvelope();
        SetupPartition(envelope);
        SetupRowLocator(null);

        var sut = CreateSut();
        var act = () => sut.DeleteAsync(envelope);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteAsync_DeletesEachDocumentFromContainer()
    {
        var envelope = CreateEnvelope();
        var locatorId = Guid.NewGuid();
        var doc1 = new ViewDocument
        {
            Id = Guid.NewGuid(), PartitionKey = "p1", View = new TestView { Id = Guid.NewGuid() }
        };
        var doc2 = new ViewDocument
        {
            Id = Guid.NewGuid(), PartitionKey = "p2", View = new TestView { Id = Guid.NewGuid() }
        };

        SetupPartition(envelope);
        SetupRowLocator(new RowLocator<TestView>(
            new KeyValuePair<string, Guid>("Id", locatorId),
            QueryOperation.EqualsValue));
        SetupPageResult(doc1, doc2);

        await CreateSut().DeleteAsync(envelope);

        await _container.Received(1).DeleteAsync<ViewDocument>(doc1.Id, doc1.PartitionKey);
        await _container.Received(1).DeleteAsync<ViewDocument>(doc2.Id, doc2.PartitionKey);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsProjectionsWithRemovedEffect()
    {
        var envelope = CreateEnvelope();
        var locatorId = Guid.NewGuid();
        var view = new TestView { Id = Guid.NewGuid() };
        var doc = new ViewDocument { Id = Guid.NewGuid(), PartitionKey = "p", View = view };

        SetupPartition(envelope);
        SetupRowLocator(new RowLocator<TestView>(
            new KeyValuePair<string, Guid>("Id", locatorId),
            QueryOperation.EqualsValue));
        SetupPageResult(doc);

        var result = await CreateSut().DeleteAsync(envelope);

        result.Should().HaveCount(1);
        result[0].Effect.Should().Be(ProjectionEffect.Removed);
        result[0].View.Should().BeSameAs(view);
    }

    [Fact]
    public async Task DeleteAsync_ExcludesDocumentsWithNullView()
    {
        var envelope = CreateEnvelope();
        var locatorId = Guid.NewGuid();
        var validView = new TestView { Id = Guid.NewGuid() };

        SetupPartition(envelope);
        SetupRowLocator(new RowLocator<TestView>(
            new KeyValuePair<string, Guid>("Id", locatorId),
            QueryOperation.EqualsValue));
        SetupPageResult(
            new ViewDocument { Id = Guid.NewGuid(), PartitionKey = "p", View = validView },
            new ViewDocument { Id = Guid.NewGuid(), PartitionKey = "p", View = null });

        var result = await CreateSut().DeleteAsync(envelope);

        result.Should().HaveCount(1);
        result[0].View.Should().BeSameAs(validView);
    }

    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_UpsertsBatchToContainer()
    {
        var envelope = CreateEnvelope();
        var view = new TestView { Id = Guid.NewGuid() };
        SetupPartition(envelope);

        await CreateSut().SaveAsync(new[] { view }, envelope);

        await _container.Received(1).UpsertBatchAsync(
            Arg.Is<IList<ViewDocument>>(docs => docs.Count == 1));
    }

    [Fact]
    public async Task SaveAsync_CreatesNewDocumentForUncachedRow()
    {
        var envelope = CreateEnvelope();
        var view = new TestView { Id = Guid.NewGuid() };
        var partition = new Partition("save-partition");
        SetupPartition(envelope, partition);

        await CreateSut().SaveAsync(new[] { view }, envelope);

        await _container.Received(1).UpsertBatchAsync(
            Arg.Is<IList<ViewDocument>>(docs =>
                docs[0].Id == view.Id &&
                docs[0].PartitionKey == "save-partition" &&
                docs[0].Type == nameof(TestView) &&
                docs[0].View == view &&
                docs[0].Sources.Contains(envelope.Subject.Value)));
    }

    [Fact]
    public async Task SaveAsync_ReusesCachedDocumentForExistingRow()
    {
        var envelope = CreateEnvelope();
        var viewId = Guid.NewGuid();
        var view = new TestView { Id = viewId };
        var partition = new Partition("cached-partition");

        SetupPartition(envelope, partition);
        SetupRowLocator(null);
        SetupPageResult(new ViewDocument
        {
            Id = viewId,
            PartitionKey = "cached-partition",
            View = view,
            Type = nameof(TestView),
            ETag = "etag-abc",
            Sources = new List<string> { envelope.Subject.Value }
        });

        var sut = CreateSut();

        await sut.GetRowsAsync(envelope);

        var updatedView = new TestView { Id = viewId };
        await sut.SaveAsync(new[] { updatedView }, envelope);

        await _container.Received(1).UpsertBatchAsync(
            Arg.Is<IList<ViewDocument>>(docs =>
                docs[0].ETag == "etag-abc" &&
                docs[0].View == updatedView));
    }

    [Fact]
    public async Task SaveAsync_AddsSubjectToSourcesWhenNotPresent()
    {
        var subject1 = CreateSubject();
        var subject2 = CreateSubject();
        var envelope1 = CreateEnvelope(subject1);
        var envelope2 = CreateEnvelope(subject2);
        var viewId = Guid.NewGuid();
        var view = new TestView { Id = viewId };
        var partition = new Partition("p");

        SetupPartition(envelope1, partition);
        SetupPartition(envelope2, partition);
        SetupRowLocator(null);
        SetupPageResult(new ViewDocument
        {
            Id = viewId,
            PartitionKey = "p",
            View = view,
            Type = nameof(TestView),
            Sources = new List<string> { subject1.Value }
        });

        var sut = CreateSut();
        await sut.GetRowsAsync(envelope1);

        await sut.SaveAsync(new[] { new TestView { Id = viewId } }, envelope2);

        await _container.Received(1).UpsertBatchAsync(
            Arg.Is<IList<ViewDocument>>(docs =>
                docs[0].Sources.Count == 2 &&
                docs[0].Sources.Contains(subject2.Value)));
    }

    [Fact]
    public async Task SaveAsync_DoesNotDuplicateExistingSubjectInSources()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject);
        var viewId = Guid.NewGuid();
        var view = new TestView { Id = viewId };
        var partition = new Partition("p");

        SetupPartition(envelope, partition);
        SetupRowLocator(null);
        SetupPageResult(new ViewDocument
        {
            Id = viewId,
            PartitionKey = "p",
            View = view,
            Type = nameof(TestView),
            Sources = new List<string> { subject.Value }
        });

        var sut = CreateSut();
        await sut.GetRowsAsync(envelope);

        await sut.SaveAsync(new[] { new TestView { Id = viewId } }, envelope);

        await _container.Received(1).UpsertBatchAsync(
            Arg.Is<IList<ViewDocument>>(docs =>
                docs[0].Sources.Count == 1));
    }

    // --- Query construction ---

    [Fact]
    public async Task GetRowsAsync_QueryIncludesTypeFilter()
    {
        var envelope = CreateEnvelope();
        SetupPartition(envelope);
        SetupRowLocator(null);
        SetupPageResult();

        await CreateSut().GetRowsAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q =>
                q.Query!.Contains("c.type = @type") &&
                q.QueryParameters["@type"] == nameof(TestView)));
    }

    [Fact]
    public async Task GetRowsAsync_UsesPageSizeOf1000()
    {
        var envelope = CreateEnvelope();
        SetupPartition(envelope);
        SetupRowLocator(null);
        SetupPageResult();

        await CreateSut().GetRowsAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q => q.PageSize == 1000));
    }

    [Fact]
    public async Task GetRowsAsync_WhenNoRowLocator_ProducesExpectedQueryFormat()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject);
        SetupPartition(envelope);
        SetupRowLocator(null);
        SetupPageResult();

        await CreateSut().GetRowsAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q =>
                q.Query == "select * from c where c.type = @type and (array_contains(c.sources, @subject))" &&
                q.QueryParameters.Count == 2));
    }

    [Fact]
    public async Task GetRowsAsync_WhenRowLocatorEquals_ProducesExpectedQueryFormat()
    {
        var subject = CreateSubject();
        var envelope = CreateEnvelope(subject);
        var locatorId = Guid.NewGuid();
        SetupPartition(envelope);
        SetupRowLocator(new RowLocator<TestView>(
            new KeyValuePair<string, Guid>("Id", locatorId),
            QueryOperation.EqualsValue));
        SetupPageResult();

        await CreateSut().GetRowsAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q =>
                q.Query == "select * from c where c.type = @type and (c.view.id = @selectValue)" &&
                q.QueryParameters.Count == 3 &&
                q.QueryParameters["@selectValue"] == locatorId.ToString()));
    }

    [Fact]
    public async Task DeleteAsync_ProducesExpectedQueryFormat()
    {
        var envelope = CreateEnvelope();
        var locatorId = Guid.NewGuid();
        SetupPartition(envelope);
        SetupRowLocator(new RowLocator<TestView>(
            new KeyValuePair<string, Guid>("Name", locatorId),
            QueryOperation.EqualsValue));
        SetupPageResult();

        await CreateSut().DeleteAsync(envelope);

        await _container.Received(1).GetPageAsync<ViewDocument>(
            Arg.Is<PartitionedPageQuery>(q =>
                q.Query == "select * from c where c.type = @type and c.view.name = @selectValue" &&
                q.QueryParameters.Count == 2 &&
                q.QueryParameters["@type"] == nameof(TestView) &&
                q.QueryParameters["@selectValue"] == locatorId.ToString()));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsCorrectPartition()
    {
        var envelope = CreateEnvelope();
        var partition = new Partition("delete-partition");
        var view = new TestView { Id = Guid.NewGuid() };

        SetupPartition(envelope, partition);
        SetupRowLocator(new RowLocator<TestView>(
            new KeyValuePair<string, Guid>("Id", Guid.NewGuid()),
            QueryOperation.EqualsValue));
        SetupPageResult(new ViewDocument { Id = Guid.NewGuid(), PartitionKey = "delete-partition", View = view });

        var result = await CreateSut().DeleteAsync(envelope);

        result[0].Partition.Should().Be(partition);
    }
}

public class TestContainerDefinition : IDocumentContainerDefinition
{
    public string Name => "test-container";
    public int TimeToLive => -1;
}