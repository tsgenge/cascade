using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.WriteModel.Policies;

public interface IPolicy
{
    bool Supports(EventEnvelope envelope);
    Task ExecuteAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
