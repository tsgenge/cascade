using CascadeEsdm.SharedKernel.ValueObjects;

namespace CascadeEsdm.WriteModel;

public interface ICommand
{
    Subject GetSubject(ICommandEnvelope envelope);
}