---
trigger: glob
globs: **/Domain/*
---
# Purpose
The aggregate provides the transactional boundary for domain write operations and enforce business rules. They contain commands which emit events.
Aggregates should be as small as possible; if a aggregate needs something to make a decision, then consider architecture of aggregates - a merge may be required.

# Standards
- Aggregates are the root entities of the domain model.
- Aggregates are containers for entities.
- Entities within an aggregate are collections of ValueObjects.
- Entities are mutable.
- Entities are exposed as public properties on the aggregate to allow mutation during event application (Hydration).
- ValueObjects are immutable.

# Folder Structure
- Aggregates should have their own directory (pluralised) in the /Domain folder.
- Aggregates feature subdirectories for Entities, details in entity-conventions.md.
- Aggregates feature a subdirectory for ValueObjects, details in valueobject-conventions.md.
- Aggregates feature a subdirectory for Commands, details in command-conventions.md.
- Aggregates feature a subdirectory for Events, details in event-conventions.md.
- Aggregates may feature a subdirectory for Services, details in service-conventions.md.
- Aggregates may feature a subdirectory for Exceptions, where aggregate specific exceptions are defined. Details in error-conventions.md.