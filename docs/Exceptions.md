# Exception Conventions

## Standards

- All exceptions should inherit from `ExceptionBase`, allowing consistent error handling across the application.
- A valid and suitable `HttpStatusCode` should be defined for each exception, or inherited from a parent exception.
- Exceptions generally occur during command execution. The nature of events means exceptions are less likely to occur during hydration or replay.
- Common exceptions (`ConflictException`, `NotFoundException`, `UnauthorisedException`, etc.) are defined in `CascadeEsdm.WriteModel.Abstractions/Exceptions`. New exceptions can be created where needed, and placed into the aggregate's `/Exceptions` directory or, where shared, into a suitable location.

---

## Endpoint Handling

Commands should ideally be executed in the API layer to ensure immediate exception feedback.

### API Handling

- API endpoints should use suitable middleware to handle thrown exceptions and translate them to HTTP responses using the exception's `HttpStatusCode`.

### Queue Consumer Processing

- Queue consumers should move messages that throw exceptions to a dead letter queue with the error message as the `DeadLetterReason`.

---

## Related Conventions

- [Aggregates](Aggregates.md)
- [Commands](Commands.md)
- [Events](Events.md)
- [Value Objects](ValueObjects.md)
