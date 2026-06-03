---
trigger: always_on
---

- All ValueObjects must be immutable and implement `IValueObject<TValueType>`.
- Use primary constructors for setting the value, or expose the value as a readonly property.
- Implement implicit conversion operators to and from the underlying primitive type for ergonomic usage.
- For ID-style ValueObjects:
  - Provide static `Empty` and `IsEmpty` semantics.
- For non-ID ValueObjects:
  - No `Empty`/`IsEmpty` semantics required.
- ValueObjects should be used as properties of entities/aggregates and commands for strong typing and domain clarity.
- When creating a new instance, use new(value) rather than new ValueObjectName(value).
- If validation is required, use a constant Pattern property and check using regex (for strings) and throw a System.ComponentModel.DataAnnotations.ValidationException on failure, or a suitable exception inheriting from ExceptionBase.