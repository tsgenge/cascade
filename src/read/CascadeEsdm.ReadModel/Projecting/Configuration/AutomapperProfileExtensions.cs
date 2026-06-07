using AutoMapper;
using CascadeEsdm.SharedKernel.Events;
using CascadeEsdm.ReadModel.Views;
using CascadeEsdm.ReadModel.Querying;
using System.Linq.Expressions;

namespace CascadeEsdm.ReadModel.Projecting.Configuration;

internal static class AutomapperProfileExtensions
{
    public static IMappingExpression<TEvent, TView> ForProperty<TEvent, TView, TMember>(this IMappingExpression<TEvent, TView> map,
        Expression<Func<TView, TMember>> destinationMember, Expression<Func<TEvent, TMember>> sourceMember)
        where TEvent : IDomainEvent
        where TView : IView
    {
        return map.ForMember(destinationMember, x => x.MapFrom(sourceMember));
    }

    public static IMappingExpression<TEvent, TView> ForProperty<TEvent, TView, TMember>(this IMappingExpression<TEvent, TView> map,
        Expression<Func<TView, TMember>> destinationMember, Func<TEvent, EventEnvelope?, TMember> sourceMember)
        where TEvent : IDomainEvent
        where TView : IView
    {
        return map.ForMember(destinationMember, x => x.MapFrom((e, d, o, c) => sourceMember(e, c.State as EventEnvelope)));
    }

    public static IMappingExpression<TEvent, TDestination> UsingRowLocator<TEvent, TDestination>(this IMappingExpression<TEvent, TDestination> map,
        Profile profile, Func<TEvent, EventEnvelope?, KeyValuePair<string, Guid>> rowLocator, QueryOperation? operation = null)
        where TEvent : IDomainEvent
        where TDestination : IView
    {
        operation ??= QueryOperation.EqualsValue;
        profile.CreateMap<TEvent, RowLocator<TDestination>>()
            .ConstructUsing((e, s) => new RowLocator<TDestination>(rowLocator(e, s.State as EventEnvelope), operation));

        return map;
    }

    public static IMappingExpression<TEvent, TDestination> AddsNewRow<TEvent, TDestination>(this IMappingExpression<TEvent, TDestination> map,
        Profile profile, Func<TEvent, EventEnvelope?, Guid> keyLocator)
        where TEvent : IDomainEvent
        where TDestination : IView
    {
        profile.CreateMap<TEvent, RowAdder<TDestination>>()
            .ConstructUsing((e, s) => new RowAdder<TDestination>(keyLocator(e, s.State as EventEnvelope)));

        return map;
    }

    public static IMappingExpression<TEvent, TDestination> RemovesRow<TEvent, TDestination>(this IMappingExpression<TEvent, TDestination> map,
        Profile profile)
        where TEvent : IDomainEvent
        where TDestination : IView
    {
        profile.CreateMap<TEvent, RowRemover<TDestination>>()
            .ConstructUsing((e, s) => new RowRemover<TDestination> { Deletes = true });

        return map;
    }

    public static IMappingExpression<TEvent, TDestination> UsingPartitionKey<TEvent, TDestination>(this IMappingExpression<TEvent, TDestination> map,
        Profile profile, Func<TEvent, EventEnvelope?, Guid> keyLocator)
        where TEvent : IDomainEvent
        where TDestination : IView
    {
        profile.CreateMap<TEvent, ExplicitPartitionKey<TDestination>>().ConstructUsing((e, s) => new ExplicitPartitionKey<TDestination>(keyLocator(e, s.State as EventEnvelope)));
        return map;
    }
}
