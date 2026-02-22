namespace Cascade.SharedKernel.ValueObjects;

public interface ISubject : IValueObject<string>
{
    Guid Id { get; }
    Guid? Parent { get; }
    string Type { get; }
}