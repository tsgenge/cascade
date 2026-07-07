using CascadeEsdm.SharedKernel.Extensions;
using CascadeEsdm.SharedKernel.ValueObjects;
using CascadeEsdm.TestDomain.People;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.ValueObjectTests;

public class SubjectTests
{
    [Fact]
    public void Constructor_FromString_ParsesSimpleSubject()
    {
        var id = Guid.NewGuid();
        var value = $"PersonAggregate/{id:n}";

        var subject = new Subject(value);

        subject.Id.Should().Be(id);
        subject.Type.Should().Be("PersonAggregate");
        subject.Parent.Should().BeNull();
        subject.Value.Should().Be(value);
    }

    [Fact]
    public void Loads_From_AuthMs()
    {
        var rawId = "SN8zzLjGhJON87Tr71Iy09cEw1B2";
        var sut = new Subject($"Authentication/{rawId}");
        sut.RawId.Should().Be(rawId);
        sut.Id.Should().Be(rawId.ToGuid());
        sut.Value.Should().Be($"Authentication/{rawId}");
    }

    [Fact]
    public void Constructor_FromString_ParsesSubjectWithParent()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var value = $"PersonAggregate/{parentId:n}/{id:n}";

        var subject = new Subject(value);

        subject.Id.Should().Be(id);
        subject.Type.Should().Be("PersonAggregate");
        subject.Parent.Should().Be(parentId);
        subject.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_WithStringId_Stores_RawId()
    {
        var id = $"cabbages-{Guid.NewGuid()}";
        var aggregate = "PersonAggregate";
        var value = $"PersonAggregate/{id}";

        var subject = new Subject(id, aggregate);

        subject.Id.Should().Be(id.ToGuid());
        subject.RawId.Should().Be(id);
        subject.Type.Should().Be("PersonAggregate");
        subject.Value.Should().Be(value);
    }

    [Fact]
    public void Constructor_FromString_InvalidFormat_ThrowsArgumentException()
    {
        var invalidValue = "invalid-format";

        Action act = () => new Subject(invalidValue);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_FromGuidComponents_CreatesSubject()
    {
        var id = Guid.NewGuid();
        var type = "PersonAggregate";

        var subject = new Subject(id, type);

        subject.Id.Should().Be(id);
        subject.Type.Should().Be(type);
        subject.Parent.Should().BeNull();
        subject.Value.Should().Be($"PersonAggregate/{id:n}");
    }

    [Fact]
    public void Constructor_FromGuidComponents_WithParent_CreatesSubject()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var type = "PersonAggregate";

        var subject = new Subject(id, type, parentId);

        subject.Id.Should().Be(id);
        subject.Type.Should().Be(type);
        subject.Parent.Should().Be(parentId);
        subject.Value.Should().Be($"PersonAggregate/{parentId:n}/{id:n}");
    }

    [Fact]
    public void Constructor_FromStringComponents_CreatesSubject()
    {
        var idString = "a2cb945bf1254fbbbedbaa0f40d75ce8";
        var expectedId = idString.ToGuid();
        var type = "PersonAggregate";

        var subject = new Subject(idString, type);

        subject.Id.Should().Be(expectedId);
        subject.Type.Should().Be(type);
        subject.Parent.Should().BeNull();
        subject.Value.Should().Be($"PersonAggregate/{idString}");
    }

    [Fact]
    public void Constructor_FromStringComponents_WithParent_CreatesSubject()
    {
        var idString = "a2cb945bf1254fbbbedbaa0f40d75ce8";
        var parentIdString = "919c73c4f0f6488934b0d1c19eb08c4b";
        var expectedId = idString.ToGuid();
        var expectedParentId = parentIdString.ToGuid();
        var type = "PersonAggregate";

        var subject = new Subject(idString, type, parentIdString);

        subject.Id.Should().Be(expectedId);
        subject.Type.Should().Be(type);
        subject.Parent.Should().Be(expectedParentId);
        subject.Value.Should().Be($"PersonAggregate{parentIdString}/{idString}");
    }

    [Fact]
    public void Constructor_FromStringComponents_WithNullParent_CreatesSubject()
    {
        var idString = "a2cb945bf1254fbbbedbaa0f40d75ce8";
        var expectedId = idString.ToGuid();
        var type = "PersonAggregate";

        var subject = new Subject(idString, type);

        subject.Id.Should().Be(expectedId);
        subject.Type.Should().Be(type);
        subject.Parent.Should().BeNull();
        subject.Value.Should().Be($"PersonAggregate/{idString}");
    }

    [Fact]
    public void Constructor_FromStringComponents_WithEmptyParent_CreatesSubjectWithoutParent()
    {
        var idString = "a2cb945bf1254fbbbedbaa0f40d75ce8";
        var expectedId = idString.ToGuid();
        var type = "PersonAggregate";

        var subject = new Subject(idString, type, "");

        subject.Id.Should().Be(expectedId);
        subject.Type.Should().Be(type);
        subject.Parent.Should().BeNull();
        subject.Value.Should().Be($"PersonAggregate/{idString}");
    }

    [Fact]
    public void ForAggregate_WithAggregate_UsesAggregateId()
    {
        var id = Guid.NewGuid();
        var aggregate = new PersonAggregate { Id = id };

        var subject = Subject.ForAggregate(aggregate);

        subject.Id.Should().Be(id);
        subject.Type.Should().Be("PersonAggregate");
    }

    [Fact]
    public void ForAggregate_WithExplicitId_UsesExplicitId()
    {
        var aggregateId = Guid.NewGuid();
        var explicitId = Guid.NewGuid();
        var aggregate = new PersonAggregate { Id = aggregateId };

        var subject = Subject.ForAggregate(aggregate, explicitId);

        subject.Id.Should().Be(explicitId);
        subject.Type.Should().Be("PersonAggregate");
    }

    [Fact]
    public void ForAggregate_WithExplicitIdAndParent_UsesBoth()
    {
        var aggregateId = Guid.NewGuid();
        var explicitId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var aggregate = new PersonAggregate { Id = aggregateId };

        var subject = Subject.ForAggregate(aggregate, explicitId, parentId);

        subject.Id.Should().Be(explicitId);
        subject.Type.Should().Be("PersonAggregate");
        subject.Parent.Should().Be(parentId);
    }

    [Fact]
    public void ForAggregate_Generic_WithExplicitId()
    {
        var id = Guid.NewGuid();

        var subject = Subject.ForAggregate<PersonAggregate>(id);

        subject.Id.Should().Be(id);
        subject.Type.Should().Be("PersonAggregate");
    }

    [Fact]
    public void ForStorage_ReturnsFormattedValue()
    {
        var id = Guid.NewGuid();
        var subject = new Subject(id, "PersonAggregate");

        var result = subject.ForStorage();

        result.Should().Be($"PersonAggregate-{id:n}");
    }

    [Fact]
    public void ForStorage_DoesNotIncludeParent()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var subject = new Subject(id, "PersonAggregate", parentId);

        var result = subject.ForStorage();

        result.Should().Be($"PersonAggregate-{id:n}");
    }
}