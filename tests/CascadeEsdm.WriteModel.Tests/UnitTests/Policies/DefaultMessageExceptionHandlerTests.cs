using CascadeEsdm.SharedKernel.Infrastructure.Messaging;
using CascadeEsdm.WriteModel.Policies;
using FluentAssertions;

namespace CascadeEsdm.WriteModel.Tests.UnitTests.Policies;

public class DefaultMessageExceptionHandlerTests
{
    [Fact]
    public async Task DefaultMessageExceptionHandler_Always_ReturnsDeadLetter()
    {
        var handler = new DefaultMessageExceptionHandler();
        var message = new Message("body", new Dictionary<string, object>());

        var result = await handler.HandleAsync(message, new InvalidOperationException("test"), CancellationToken.None);

        result.Should().Be(MessageAction.DeadLetter);
    }
}
