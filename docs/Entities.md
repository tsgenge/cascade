# Entities, with a capital E
Ah, entities. We've collectively spent the last 30 years getting tangled up with entities - spreading them everywhere, making them into god models that turn out systems into regret filled spaghetti code. It's time to _get them back in their box_ and stop being so obsessed.

They represent state of the aggregate at the hydration point, a means to organise hydrated events into structured and focused models that make sense for the domain.

## Standards
- An entity is mutable.
- An entity properties are _not_ primitives.
- An entity properties should be immutable value objects.
- An entity should not be referenced or accessed outside of its aggregate root (the aggregate folder).
- An entities state is built by replaying all events from a snapshot or from the stream start.
- The entities are available as properties on the aggregate.
- The entity should not contain logic or rules (value objects do that).
- Entities should exist in the /Entities folder of the aggregate.
- Command Executors do not change entities. They use entities to determine state and emit events.
- Event Appliers do change entities. This is what loads the state**.

** Part of cascades hydration process.