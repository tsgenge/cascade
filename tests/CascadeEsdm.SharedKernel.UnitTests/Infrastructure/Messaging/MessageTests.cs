using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using FluentAssertions;

namespace CascadeEsdm.SharedKernel.UnitTests.Infrastructure.Messaging;

public class MessageTests
{
    [Fact]
    public void Constructor_SetsBodyAndApplicationProperties()
    {
        var body = "{\"key\":\"value\"}";
        var properties = new Dictionary<string, object> { { "prop1", "val1" } };

        var message = new Message(body, properties);

        message.Body.Should().Be(body);
        message.ApplicationProperties.Should().BeEquivalentTo(properties);
    }

    [Fact]
    public void Constructor_WithEmptyProperties_SetsEmptyDictionary()
    {
        var properties = new Dictionary<string, object>();

        var message = new Message("body", properties);

        message.ApplicationProperties.Should().BeEmpty();
    }
}
