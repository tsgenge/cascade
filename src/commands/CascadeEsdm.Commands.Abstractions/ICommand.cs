using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.Commands;

public interface ICommand
{
    ISubject GetSubject(ICommandEnvelope envelope);
}

public interface IAddCommand : ICommand
{
    Guid GetParentId(ICommandEnvelope envelope);
}