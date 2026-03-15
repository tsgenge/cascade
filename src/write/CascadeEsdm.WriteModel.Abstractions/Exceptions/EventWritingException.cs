using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class EventWritingException : ExceptionBase
{
    public EventWritingException(Exception inner) : base("Unable to write command events to stream.", inner)
    {

    }
}
