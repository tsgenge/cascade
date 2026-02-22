using Cascade.SharedKernel.ValueObjects;

namespace Cascade.Commands;

public interface ICommand
{
    ISubject GetSubject(ICommandEnvelope envelope);
}

public interface IAddCommand : ICommand
{
    Guid GetParentId(ICommandEnvelope envelope);
}