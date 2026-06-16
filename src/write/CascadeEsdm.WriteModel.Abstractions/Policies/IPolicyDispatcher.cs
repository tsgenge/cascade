using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.WriteModel.Policies;

public interface IPolicyDispatcher
{
    Task DispatchAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
