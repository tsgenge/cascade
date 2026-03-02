using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.Commands.Exceptions;

public class UnknownEventException : ExceptionBase
{
    public string Event { get; private set; }
    public string AggregateSource { get; private set; }
    public UnknownEventException(string @event, string aggregateSource) : base(400)
    {
        Event = @event;
        AggregateSource = aggregateSource;
    }
}