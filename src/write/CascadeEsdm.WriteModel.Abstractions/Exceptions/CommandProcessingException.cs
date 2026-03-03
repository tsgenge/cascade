using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class CommandProcessingException : ExceptionBase
{
    public CommandProcessingException(Exception inner) : base("The command failed to process successfully.", inner)
    {

    }
}