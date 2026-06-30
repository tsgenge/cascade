using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace CascadeEsdm.WriteModel.CommandHandling;

internal abstract class LoggingCommandHandlerDecoratorBase<TCommand>
    where TCommand : ICommand
{
    private readonly ILogger _logger;
    private readonly ITelemetryLogger _telemetryLogger;

    protected LoggingCommandHandlerDecoratorBase(ITelemetryLogger telemetryLogger, ILogger logger)
    {
        _telemetryLogger = telemetryLogger ?? throw new ArgumentNullException(nameof(telemetryLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected IDisposable? BeginScope(ICommandEnvelope<TCommand> command)
    {
        var subject = command.Command.GetSubject(command);
        return _logger.BeginScope("Commencing execution of command {Command} againsts {AggregateType} ({AggregateId}) for user {User} in Organisation {Organisation}",
            command.Type,
            subject.Type,
            subject.Id,
            command.SecurityContext.User.Id,
            command.SecurityContext.Tenant.Value);
    }

    protected IDisposable CreateOperation()
    {
        return _telemetryLogger.StartOperation($"Executing {typeof(TCommand).Name}");
    }

    protected void LogError(Exception ex)
    {
        _logger.LogError(ex, "Command failed.");
    }
}

internal class LoggingCommandHandlerDecorator<TCommand> : LoggingCommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private readonly ICommandHandler<TCommand> _inner;

    public LoggingCommandHandlerDecorator(ICommandHandler<TCommand> inner, ITelemetryLogger telemetryLogger, ILogger<LoggingCommandHandlerDecorator<TCommand>> logger)
        : base(telemetryLogger, logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ICommandResponse> HandleAsync(ICommandEnvelope<TCommand> command)
    {
        using var op = CreateOperation();
        using var scope = BeginScope(command);

        try {
            return await _inner.HandleAsync(command);
        }
        catch (Exception ex) {
            LogError(ex);
            throw;
        }
    }
}

internal class LoggingCommandHandlerDecorator<TCommand, TResponse> : LoggingCommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand
    where TResponse : ICommandResponse
{
    private readonly ICommandHandler<TCommand, TResponse> _inner;

    public LoggingCommandHandlerDecorator(ICommandHandler<TCommand, TResponse> inner, ITelemetryLogger telemetryLogger, ILogger<LoggingCommandHandlerDecorator<TCommand>> logger)
        : base(telemetryLogger, logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<TResponse> HandleAsync(ICommandEnvelope<TCommand> command)
    {
        using var op = CreateOperation();
        using var scope = BeginScope(command);

        try {
            return await _inner.HandleAsync(command);
        }
        catch (Exception ex) {
            LogError(ex);
            throw;
        }
    }
}