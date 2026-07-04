using System.Threading.Channels;

namespace CascadeEsdm.Testing;

public class MessageChannel<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public async Task<T> WaitForNextAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        return await _channel.Reader.ReadAsync(cts.Token);
    }

    public async Task PublishAsync(T message, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public Task Clear()
    {
        while (_channel.Reader.TryRead(out _)) { }
        return Task.CompletedTask;
    }
}