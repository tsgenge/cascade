using CascadeEsdm.SharedKernel.Querying;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Querying;

public class PageResultTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var page = new List<string> { "item1", "item2" }.AsReadOnly();
        var continuationToken = new PageContinuationToken("next-token");
        var container = new PagedResultContainer("container-1");

        var result = new PageResult<string>(page, continuationToken, container);

        result.Page.Should().BeEquivalentTo(page);
        result.ContinuationToken.Should().Be(continuationToken);
        result.Container.Should().Be(container);
    }

    [Fact]
    public void Constructor_WithEmptyPage_SetsEmptyList()
    {
        var page = new List<string>().AsReadOnly();
        var continuationToken = new PageContinuationToken(null);
        var container = new PagedResultContainer("container");

        var result = new PageResult<string>(page, continuationToken, container);

        result.Page.Should().BeEmpty();
    }

    [Fact]
    public void Implements_IContinuousPageResult()
    {
        var page = new List<int> { 1, 2, 3 }.AsReadOnly();
        var token = new PageContinuationToken("token");
        var container = new PagedResultContainer("c");

        var result = new PageResult<int>(page, token, container);

        result.Should().BeAssignableTo<IContinuousPageResult<int>>();
    }

    [Fact]
    public void Implements_IPageResult()
    {
        var page = new List<int> { 1 }.AsReadOnly();
        var token = new PageContinuationToken(null);
        var container = new PagedResultContainer("c");

        var result = new PageResult<int>(page, token, container);

        result.Should().BeAssignableTo<IPageResult<int>>();
    }
}
