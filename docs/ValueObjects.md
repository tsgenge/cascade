# Value Object Conventions

## Standards

- All ValueObjects must be immutable and implement `IValueObject<TValueType>`.
- Use primary constructors for setting the value, or expose the value as a readonly property.
- Implement implicit conversion operators to and from the underlying primitive type for ergonomic usage.
- ValueObjects should be used as properties of entities/aggregates and commands for strong typing and domain clarity.
- When creating a new instance, use `new(value)` rather than `new ValueObjectName(value)`.

---

## ID-Style ValueObjects

For ValueObjects that represent identifiers:
- Provide static `Empty` and `IsEmpty` semantics.

```csharp
public record OrderId(Guid Value) : IValueObject<Guid>
{
    public static OrderId Empty => new(Guid.Empty);
    public static bool IsEmpty(OrderId id) => id.Value == Guid.Empty;

    public static implicit operator OrderId(Guid value) => new(value);
    public static implicit operator Guid(OrderId id) => id.Value;
}
```

---

## Non-ID ValueObjects

For ValueObjects that represent domain values (names, descriptions, etc.):
- No `Empty`/`IsEmpty` semantics required.
- If validation is required, use a constant `Pattern` property and check using regex (for strings). Throw a `System.ComponentModel.DataAnnotations.ValidationException` on failure, or a suitable exception inheriting from `ExceptionBase`.

```csharp
public record EmailAddress(string Value) : IValueObject<string>
{
    private const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public EmailAddress(string value) : this(value)
    {
        if (!Regex.IsMatch(value, Pattern))
            throw new ValidationException($"Invalid email address: {value}");
    }

    public static implicit operator EmailAddress(string value) => new(value);
    public static implicit operator string(EmailAddress vo) => vo.Value;
}
```

---

## Related Conventions

- [Aggregates](Aggregates.md)
- [Commands](Commands.md)
- [Events](Events.md)
- [Exceptions](Exceptions.md)
