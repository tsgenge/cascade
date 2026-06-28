using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.SharedKernel.Querying;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Infrastructure.Storage;

public class PartitionedPageQueryTests
{
    [Fact]
    public void Constructor_SetsPartitionKeyAndParameters()
    {
        var originalQuery = new PageQuery("SELECT * FROM c", 10, "token", "Name", true, false);
        var partitionKey = "pk-123";
        var parameters = new Dictionary<string, string> { { "param1", "value1" } };

        var query = new PartitionedPageQuery(originalQuery, partitionKey, parameters);

        query.PartitionKey.Should().Be(partitionKey);
        query.QueryParameters.Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void Constructor_InheritsOriginalQueryProperties()
    {
        var originalQuery = new PageQuery("SELECT * FROM c", 25, "cont-token", "Date", true, true);
        var parameters = new Dictionary<string, string>();

        var query = new PartitionedPageQuery(originalQuery, "pk", parameters);

        query.Query.Should().Be("SELECT * FROM c");
        query.Size.Should().Be(25);
        query.ContinuationToken.Should().Be("cont-token");
        query.OrderBy.Should().Be("Date");
        query.Descending.Should().BeTrue();
        query.Deleted.Should().BeTrue();
    }

    [Fact]
    public void PartitionKey_CanBeSet()
    {
        var originalQuery = new PageQuery(null, 10);
        var query = new PartitionedPageQuery(originalQuery, "original", new Dictionary<string, string>());

        query.PartitionKey = "updated";

        query.PartitionKey.Should().Be("updated");
    }

    [Fact]
    public void QueryParameters_CanBeSet()
    {
        var originalQuery = new PageQuery(null, 10);
        var query = new PartitionedPageQuery(originalQuery, "pk", new Dictionary<string, string>());
        var newParams = new Dictionary<string, string> { { "key", "value" } };

        query.QueryParameters = newParams;

        query.QueryParameters.Should().BeEquivalentTo(newParams);
    }
}
