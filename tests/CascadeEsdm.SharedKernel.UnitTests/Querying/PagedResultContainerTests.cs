using CascadeEsdm.SharedKernel.Querying;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Querying;

public class PagedResultContainerTests
{
    [Fact]
    public void Constructor_SetsValue()
    {
        var container = new PagedResultContainer("my-container");

        container.Value.Should().Be("my-container");
    }

    [Fact]
    public void ImplicitOperator_ToString_ReturnsValue()
    {
        var container = new PagedResultContainer("container-name");

        string result = container;

        result.Should().Be("container-name");
    }
}
