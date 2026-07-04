using CascadeEsdm.WriteModel;

namespace CascadeEsdm.TestDomain.People;

public class ExecutorPayload<TCommand>
    where TCommand : ICommand
{
    public ICommandEnvelope<TCommand>? Envelope { get; set; }
}