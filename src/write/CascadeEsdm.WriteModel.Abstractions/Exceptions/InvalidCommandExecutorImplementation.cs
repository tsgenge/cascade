using CascadeEsdm.SharedKernel.Exceptions;

namespace CascadeEsdm.WriteModel.Exceptions;

public class InvalidCommandExecutorImplementation : ExceptionBase
{
    public InvalidCommandExecutorImplementation(string commandName) : base(
        $"The executor for {commandName} did not set Subject, Source or Channel correctly in emitted events. Use CommandEnvelope.CreateEvent in the implementation.") { }
}