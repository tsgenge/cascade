using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class MissingExecutorException : ExceptionBase
{
    public string[] MissingExecutors { get; }
    public MissingExecutorException(string[] missingExecutors) : base("Commands are missing executors.")
    {
        MissingExecutors = missingExecutors;
    }
}