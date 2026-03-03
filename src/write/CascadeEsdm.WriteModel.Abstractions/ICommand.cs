using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel;

public interface ICommand
{
    ISubject GetSubject(ICommandEnvelope envelope);
}

public interface IAddCommand : ICommand
{
    Guid GetParentId(ICommandEnvelope envelope);
}