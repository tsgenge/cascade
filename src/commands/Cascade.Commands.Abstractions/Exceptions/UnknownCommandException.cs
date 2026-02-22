using Cascade.SharedKernel.Exceptions;

namespace Cascade.Commands.Exceptions;

public class UnknownCommandException : ExceptionBase
{
    public string Command { get; private set; }
    public string AggregateSource { get; private set; }

    public UnknownCommandException(string command, string aggregateSource) : base(400)
    {
        Command = command;
        AggregateSource = aggregateSource;
    }
}
