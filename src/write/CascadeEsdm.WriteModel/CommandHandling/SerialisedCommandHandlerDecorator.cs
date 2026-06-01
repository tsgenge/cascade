using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;
using System.Collections.Concurrent;
using System.Reflection;

namespace CascadeEsdm.WriteModel.CommandHandling;

internal class SerialisedCommandHandlerDecorator<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand
    where TResponse : ICommandResponse
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ConcurrentDictionary<Type, CommandLockLevel?> LockCache = new();
    private readonly ICommandHandler<TCommand, TResponse> _inner;
    private readonly IDistributedLockProvider _lockProvider;

    public SerialisedCommandHandlerDecorator(ICommandHandler<TCommand, TResponse> inner, IDistributedLockProvider lockProvider)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
    }

    public async Task<TResponse> HandleAsync(ICommandEnvelope<TCommand> command)
    {
        await using var @lock = await GetLockIfRequiredAsync(command);

        return await _inner.HandleAsync(command);
    }

    private bool RequiresLock(out CommandLockLevel lockType)
    {
        if (!LockCache.TryGetValue(typeof(TCommand), out var commandLockLevel)) {
            var attribute = typeof(TCommand).GetCustomAttribute<CommandLockAttribute>();
            lockType = attribute?.Level ?? CommandLockLevel.Command;
            LockCache.TryAdd(typeof(TCommand), attribute?.Level);
            return attribute != null;
        }

        lockType = commandLockLevel ?? CommandLockLevel.Command;
        return commandLockLevel != null;
    }

    private async Task<IDistributedLock?> GetLockIfRequiredAsync(ICommandEnvelope<TCommand> envelope)
    {
        if (RequiresLock(out var lockType)) {
            var lockName = GetLockName(envelope, lockType);
            return await _lockProvider.AcquireLockAsync(lockName);
        }

        return null;
    }

    private string GetLockName(ICommandEnvelope<TCommand> envelope, CommandLockLevel level)
    {
        return level switch
        {
            CommandLockLevel.Command => $"{envelope.Command.GetSubject(envelope).ForStorage()}-{typeof(TCommand).Name}",
            _ => $"{envelope.Command.GetSubject(envelope).ForStorage()}"
        };
    }
}

internal class SerialisedCommandHandlerDecorator<TCommand> : SerialisedCommandHandlerDecorator<TCommand, ICommandResponse>, ICommandHandler<TCommand>
    where TCommand : ICommand
{
    public SerialisedCommandHandlerDecorator(ICommandHandler<TCommand> inner, IDistributedLockProvider lockProvider) : base(inner, lockProvider) { }
}

public enum CommandLockLevel
{
    /// <summary>
    ///     Locks are per command; two commands of different types can execute at once.
    /// </summary>
    Command,

    /// <summary>
    ///     All commands for the aggregate share the same lock.
    /// </summary>
    Aggregate
}

[AttributeUsage(AttributeTargets.Class)]
public class CommandLockAttribute : Attribute
{
    public CommandLockLevel Level { get; }

    public CommandLockAttribute(CommandLockLevel level)
    {
        Level = level;
    }
}