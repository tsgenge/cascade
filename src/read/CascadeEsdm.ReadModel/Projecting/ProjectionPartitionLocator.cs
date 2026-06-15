using AutoMapper;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.ReadModel.ValueObjects;
using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.ReadModel.Projecting;

public interface IProjectionPartitionLocator<TView>
{
    Partition GetPartition(EventEnvelope @event);
}

internal class ProjectionPartitionLocator<TView> : ViewPartitionLocator<TView>, IProjectionPartitionLocator<TView>
{
    private readonly IMapper _mapper;

    public ProjectionPartitionLocator(IMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public Partition GetPartition(EventEnvelope @event)
    {
        return GetPartition(@event.SecurityContext, () =>
        {
            var partitionKey = GetKeyForEnvelope(@event);
            if (partitionKey.Key.Equals(Guid.Empty))
                partitionKey = _mapper.Map<ExplicitPartitionKey<TView>>(@event.Event, o => o.State = @event);
            return partitionKey.ToString();
        });
    }

    private ExplicitPartitionKey<TView> GetKeyForEnvelope(EventEnvelope envelope)
    {
        try {
            return _mapper.Map<ExplicitPartitionKey<TView>>(envelope, o => o.State = envelope);
        }
        catch {
            return new ExplicitPartitionKey<TView>(Guid.Empty);
        }
    }
}