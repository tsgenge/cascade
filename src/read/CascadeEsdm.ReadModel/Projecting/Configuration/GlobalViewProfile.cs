using AutoMapper;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal class GlobalViewProfile : Profile
{
    public GlobalViewProfile()
    {
        CreateMap<Guid, string>().ConvertUsing(x => x.ToString("n"));
        CreateMap<IDomainEvent, IView>()
            .ForMember(x => x.Id, x => x.Ignore())
            .ForMember(x => x.Modified, x => x.MapFrom((e, v, o, s) => (s?.State as EventEnvelope)?.Time ?? DateTimeOffset.UtcNow))
            .IncludeAllDerived();

        CreateMap<EventEnvelope, IAuthoredView>()
            .ForMember(x => x.Author, x => x.Ignore())
            .IncludeAllDerived();

        CreateMap<IDomainEvent, IAuthoredView>()
            .ForMember(x => x.Id, x => x.Ignore())
            .ForMember(x => x.Author, x => x.Ignore())
            .IncludeAllDerived();
    }
}
