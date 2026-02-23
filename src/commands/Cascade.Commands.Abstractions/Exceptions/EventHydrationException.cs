using Cascade.SharedKernel.Exceptions;

namespace Cascade.Commands.Exceptions;

public class EventHydrationException : ExceptionBase
{
    public EventHydrationException(Exception inner, Type eventType, Type aggregate) : base($"Event hydration failed for {eventType.Name} in aggregate {aggregate.Name}.", inner)
    {

    }
}