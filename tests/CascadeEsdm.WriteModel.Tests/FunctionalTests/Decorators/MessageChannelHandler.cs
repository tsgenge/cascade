using CascadeEsdm.Testing;
using CascadeEsdm.WriteModel.CommandHandling;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Decorators;

public class MessageChannelHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly MessageChannel<TCommand> _channel;
    private readonly ICommandHandler<TCommand> _inner;

    public MessageChannelHandler(MessageChannel<TCommand> channel, ICommandHandler<TCommand> inner)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ICommandResponse> HandleAsync(ICommandEnvelope<TCommand> envelope)
    {
        try {
            return await _inner.HandleAsync(envelope);
        }
        finally {
            await _channel.PublishAsync(envelope.Command);
        }
    }
}