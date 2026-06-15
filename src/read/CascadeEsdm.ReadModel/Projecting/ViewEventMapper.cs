using AutoMapper;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.SharedKernel.Events;

namespace CascadeEsdm.ReadModel.Projecting;

internal interface IViewEventMapper<TView>
{
    void Map(TView rowView, EventEnvelope @event);
    Guid GetNewRowId(EventEnvelope @event);
}

internal class ViewEventMapper<TView> : IViewEventMapper<TView>
{
    private readonly IMapper _mapper;

    public ViewEventMapper(IMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public Guid GetNewRowId(EventEnvelope envelope)
    {
        var creator = _mapper.Map<RowAdder<TView>?>(envelope.Event, o => o.State = envelope);
        return creator?.NewRowId ?? Guid.NewGuid();
    }

    public void Map(TView rowView, EventEnvelope envelope)
    {
        _mapper.Map(envelope.Event, rowView, o => o.State = envelope);
    }
}