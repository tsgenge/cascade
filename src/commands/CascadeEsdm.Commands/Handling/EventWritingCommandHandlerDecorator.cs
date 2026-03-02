using CascadeEsdm.Commands.Abstractions.Handling;
using CascadeEsdm.Commands.EventStream;
using CascadeEsdm.Commands.Exceptions;

namespace CascadeEsdm.Commands.Handling;

internal abstract class EventWritingCommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly IEventStreamWriter _eventWriter;

    protected EventWritingCommandHandlerDecoratorBase(IEventStreamWriter eventWriter)
    {
        _eventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));
    }

    protected async Task WriteEventsAsync(ICommandResponse response)
    {
        foreach (var evt in response.Events)
        {
            _eventWriter.Add(evt);
        }

        await _eventWriter.SaveAsync();
    }
}

internal class EventWritingCommandHandlerDecorator<TCommand, TResponse> : EventWritingCommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand
    where TResponse : CommandResponse
{
    private readonly ICommandHandler<TCommand, TResponse> _inner;

    public EventWritingCommandHandlerDecorator(ICommandHandler<TCommand, TResponse> inner, IEventStreamWriter eventWriter)
        : base(eventWriter)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<TResponse> HandleAsync(ICommandEnvelope<TCommand> command)
    {
        try
        {
            var response = await _inner.HandleAsync(command);
            await WriteEventsAsync(response);
            return response;
        }
        catch (EventWritingException ex)
        {
            throw new CommandProcessingException(ex);
        }
    }
}

internal class EventWritingCommandHandlerDecorator<TCommand> : EventWritingCommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _inner;

    public EventWritingCommandHandlerDecorator(ICommandHandler<TCommand> inner, IEventStreamWriter eventWriter)
        : base(eventWriter)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ICommandResponse> HandleAsync(ICommandEnvelope<TCommand> command)
    {
        try
        {
            var response = await _inner.HandleAsync(command);
            await WriteEventsAsync(response);
            return response;
        }
        catch (EventWritingException ex)
        {
            throw new CommandProcessingException(ex);
        }
    }
}