namespace Cascade.SharedKernel.ValueObjects;

public interface IEventSource : IValueObject<string>
{
    string Aggregate { get; }
    Guid CommandId { get; }
    string Command { get; }
}