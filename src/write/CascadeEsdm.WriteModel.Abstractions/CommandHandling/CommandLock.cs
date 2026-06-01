namespace CascadeEsdm.WriteModel.CommandHandling;

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
    public CommandLockAttribute(CommandLockLevel level)
    {
        Level = level;
    }

    public CommandLockLevel Level { get; }
}