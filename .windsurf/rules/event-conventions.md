---
trigger: glob
globs: **/Events/*
---


## Standards
- Events are immutable record objects representing historical facts.
- Use primitive types for all event properties (do not use value objects).
- We do not use value objects for events because the data does not need the benefits of ValueObjects - validation, logic; these are statements of historical fact, so there is no need to transform or validate.
- All events inherit from `IDomainEvent` marker interface and are public.
- Use primary constructors to enforce value provision at creation.
- Do not include validation or encapsulated logic—events represent truths, not intentions.
- Events do not need to define metadata such as Id or Time or Subject - these are stored on the EventEnvelope.
- Place events in the `Events` folder under their respective aggregate.
- Events are emitted by CommandExecutors during command execution.

## Naming
- Name events in the past tense (e.g., `WorkItemCommentAdded`) using a NounVerb pattern. They should not feature the word "Event". They should be the past tense version of the command, where possible.
- Events should not feature CRUD verbs, such as Create, instead favouring Add, Change, Remove.

## Aggregate Hydration using events
- Events are ingested into the aggregate Hydration from the event stream source. This occurs usually during Command execution in the CommandHandler base (src\write\CascadeEsdm.WriteModel\CommandHandling\CommandHandler.cs) and is handled by the framework.
- The IAggregateHydrator<TAggregate> implementation is used to form the aggregate. Internally this pulls events in from the event stream, resolves the IEventApplier<TEvent, TAggregate> for each event, and applies the event to the aggregate.
- The implementation for how this is orchestrated is in the class src\write\CascadeEsdm.WriteModel\Hydration\AggregateFactory.cs.

## Event Appliers
- Implement `EventAppliers<TEvent, TAggregate>` in the same file as the event. The applier will use the supplied aggregate (with public properties with entities) and amend the aggregate directly.
- The EventApplier should be implemented as an internal class.
- The EventApplier will apply the event data to the aggregate, usually by mutating an entity in the aggregate.
- When setting the ValueObject properties of an entity during applier execution, remember to use new() reduce reduce `using` statements, rather than explicitly describing the ValueObject type.

``` csharp
    aggregate.Person.FirstName = new (@event.FirstName);

    \\ Rather than...
    aggregate.Person.FirstName = new FirstName(@event.FirstName);
```

- The EventApplier does not need (and indeed should not) change the LastSequence property of the aggregate.
- EventAppliers are discovered and registered in the Composition Root for use by the IEventApplierFactory during Hydration. This registration is not a concern of the EventApplier itself.
- EventAppliers should be optimistic in approach - since they are replaying historical events, they do not need to verify or validate using if statements. For example, this is not necessary on a PersonFirstNameChanged event;

``` csharp
internal class PersonFirstNameChangedApplier : IEventApplier<PersonFirstNameChanged, PersonAggregate>
{
    public void Apply(PersonAggregate aggregate, PersonFirstNameChanged @event, IEventEnvelope envelope)
    {
        // Since this is a replay of a historical event, we don't need to verify or validate
        // The event data is already valid and we can directly apply it
        if (aggregate.Person != null)
        {
            aggregate.Person.FirstName = new FirstName(@event.FirstName);
        }
    }
}
```
