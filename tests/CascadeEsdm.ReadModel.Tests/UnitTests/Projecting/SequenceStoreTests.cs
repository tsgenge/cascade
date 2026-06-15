using CascadeEsdm.ReadModel.Projecting;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.ValueObjects;
using FluentAssertions;
using NSubstitute;
using static CascadeEsdm.ReadModel.Projecting.SequenceStore<CascadeEsdm.ReadModel.UnitTests.UnitTests.Projecting.TestView>;

namespace CascadeEsdm.ReadModel.UnitTests.UnitTests.Projecting;

public class SequenceStoreTests
{
    private readonly ITableStore<SequenceEntity> _table = Substitute.For<ITableStore<SequenceEntity>>();

    private SequenceStore<TestView> CreateSut() => new SequenceStore<TestView>(_table);

    private static Subject CreateSubject() => new(Guid.NewGuid(), "TestAggregate");

    [Fact]
    public async Task GetLastSequenceAsync_WhenNoExistingEntity_ReturnsInitialSequence()
    {
        var subject = CreateSubject();
        _table.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((SequenceEntity?)null);

        var result = await CreateSut().GetLastSequenceAsync(subject);

        result.Subject.Should().Be(subject);
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task GetLastSequenceAsync_WhenEntityExists_ReturnsStoredSequence()
    {
        var subject = CreateSubject();
        var when = DateTimeOffset.UtcNow.AddMinutes(-5);
        _table.GetAsync("testview", subject.ForStorage())
            .Returns(new SequenceEntity
            {
                PartitionKey = "testview",
                RowKey = subject.ForStorage(),
                When = when,
                Value = 42
            });

        var result = await CreateSut().GetLastSequenceAsync(subject);

        result.Subject.Should().Be(subject);
        result.Value.Should().Be(42);
        result.UtcWhen.Should().Be(when);
    }

    [Fact]
    public async Task GetLastSequenceAsync_CachesEntity_DoesNotQueryTwice()
    {
        var subject = CreateSubject();
        _table.GetAsync("testview", subject.ForStorage())
            .Returns(new SequenceEntity
            {
                PartitionKey = "testview",
                RowKey = subject.ForStorage(),
                When = DateTimeOffset.UtcNow,
                Value = 10
            });

        var sut = CreateSut();
        await sut.GetLastSequenceAsync(subject);
        await sut.GetLastSequenceAsync(subject);

        await _table.Received(1).GetAsync("testview", subject.ForStorage());
    }

    [Fact]
    public async Task SaveAsync_WhenNoPriorCache_UpsertWithWildcardETag()
    {
        var subject = CreateSubject();
        var sequence = new Sequence(subject, DateTimeOffset.UtcNow, 5);

        await CreateSut().SaveAsync(sequence);

        await _table.Received(1).UpsertAsync(Arg.Is<SequenceEntity>(e =>
            e.PartitionKey == "testview" &&
            e.RowKey == subject.ForStorage() &&
            e.Value == 5 &&
            e.ETag == "*"));
    }

    [Fact]
    public async Task SaveAsync_WhenCachedEntity_UpsertWithCachedETag()
    {
        var subject = CreateSubject();
        _table.GetAsync("testview", subject.ForStorage())
            .Returns(new SequenceEntity
            {
                PartitionKey = "testview",
                RowKey = subject.ForStorage(),
                When = DateTimeOffset.UtcNow,
                Value = 3,
                ETag = "etag-123"
            });

        var sut = CreateSut();
        await sut.GetLastSequenceAsync(subject);

        var sequence = new Sequence(subject, DateTimeOffset.UtcNow, 4);
        await sut.SaveAsync(sequence);

        await _table.Received(1).UpsertAsync(Arg.Is<SequenceEntity>(e =>
            e.ETag == "etag-123" &&
            e.Value == 4));
    }

    [Fact]
    public async Task SaveAsync_ClearsCache()
    {
        var subject = CreateSubject();
        _table.GetAsync("testview", subject.ForStorage())
            .Returns(new SequenceEntity
            {
                PartitionKey = "testview",
                RowKey = subject.ForStorage(),
                When = DateTimeOffset.UtcNow,
                Value = 1
            });

        var sut = CreateSut();
        await sut.GetLastSequenceAsync(subject);
        await sut.SaveAsync(new Sequence(subject, DateTimeOffset.UtcNow, 2));
        await sut.GetLastSequenceAsync(subject);

        await _table.Received(2).GetAsync("testview", subject.ForStorage());
    }

    [Fact]
    public async Task PartitionKey_IsDerivedFromViewTypeName()
    {
        var subject = CreateSubject();
        _table.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((SequenceEntity?)null);

        await CreateSut().GetLastSequenceAsync(subject);

        await _table.Received(1).GetAsync("testview", Arg.Any<string>());
    }

    [Fact]
    public async Task RowKey_IsDerivedFromSubjectForStorage()
    {
        var subject = CreateSubject();
        var expectedRowKey = subject.ForStorage();
        _table.GetAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((SequenceEntity?)null);

        await CreateSut().GetLastSequenceAsync(subject);

        await _table.Received(1).GetAsync(Arg.Any<string>(), expectedRowKey);
    }

}
