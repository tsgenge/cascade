using Cascade.SharedKernel.Exceptions;

namespace Cascade.Commands.Exceptions;

public class EventWritingException : ExceptionBase
{
    public EventWritingException(Exception inner) : base("Unable to write command events to stream.", inner)
    {

    }
}
