---
trigger: always_on
---
- All exceptions should inherit from ExceptionBase, allowing consistent error handling across the application.
- ExceptionBase can be found in src\shared\CascadeEsdm.SharedKernel.Abstractions\Exceptions\ExceptionBase.cs
- A valid and suitable HttpStatus code should be defined for each exception, or inherited from a parent exception.
- Exceptions generally occur during command execution. The nature of events should mean exceptions are less likely to occur during hydration or replay.
- A common set of exceptions are defined in src\write\CascadeEsdm.WriteModel.Abstractions\Exceptions. New exceptions can be created where needed, and placed into the aggregates /Exceptions directory or where shared into a suitable location.

## Endpoint Handling
Commands should ideally be executed in the API layer to ensure immediate exception feedback.

### API handling
- API endpoints should use suitable middleware to handle thrown exceptions.

### Queue Consumer Processing
- Queue consumers should move messages that throw exceptions to a dead letter queue with the error message as the DeadLetterReason.