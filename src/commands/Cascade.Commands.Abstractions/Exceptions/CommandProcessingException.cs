using Cascade.SharedKernel.Exceptions;

namespace Cascade.Commands.Exceptions;

public class CommandProcessingException : ExceptionBase
{
    public CommandProcessingException(Exception inner) : base("The command failed to process successfully.", inner)
    {

    }
}