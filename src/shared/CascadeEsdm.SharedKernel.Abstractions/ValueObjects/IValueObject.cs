namespace CascadeEsdm.SharedKernel.ValueObjects;

public interface IValueObject;

public interface IValueObject<out TValue> : IValueObject
{
    public TValue Value { get; }
}