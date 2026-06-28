using CascadeEsdm.SharedKernel.Querying;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Querying;

public class PageQueryTests
{
    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var query = new PageQuery("SELECT * FROM c", 25, "cont-token", "Name", true, true);

        query.Query.Should().Be("SELECT * FROM c");
        query.Size.Should().Be(25);
        query.ContinuationToken.Should().Be("cont-token");
        query.OrderBy.Should().Be("Name");
        query.Descending.Should().BeTrue();
        query.Deleted.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDefaults_SetsDefaultValues()
    {
        var query = new PageQuery("query", 10);

        query.ContinuationToken.Should().BeNull();
        query.OrderBy.Should().BeNull();
        query.Descending.Should().BeFalse();
        query.Deleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNullQuery_SetsNull()
    {
        var query = new PageQuery(null, 10);

        query.Query.Should().BeNull();
    }

    [Fact]
    public void Copy_WithNewQuery_PreservesOtherProperties()
    {
        var original = new PageQuery("original", 25, "token", "Date", true, true);

        var copy = original.Copy("new-query");

        copy.Query.Should().Be("new-query");
        copy.Size.Should().Be(25);
        copy.ContinuationToken.Should().Be("token");
        copy.OrderBy.Should().Be("Date");
        copy.Descending.Should().BeTrue();
        copy.Deleted.Should().BeTrue();
    }

    [Fact]
    public void Copy_WithNullQuery_SetsNullQuery()
    {
        var original = new PageQuery("original", 10);

        var copy = original.Copy(null);

        copy.Query.Should().BeNull();
        copy.Size.Should().Be(10);
    }

    [Fact]
    public void Implements_IContinuousPageQuery()
    {
        var query = new PageQuery("q", 10, "token");

        query.Should().BeAssignableTo<IContinuousPageQuery>();
        (query as IContinuousPageQuery)!.ContinuationToken.Should().Be("token");
    }

    [Fact]
    public void Implements_IPageQuery()
    {
        var query = new PageQuery("q", 10);

        query.Should().BeAssignableTo<IPageQuery>();
    }
}
