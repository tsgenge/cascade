using AutoMapper;
using CascadeEsdm.ReadModel.Projecting;
using CascadeEsdm.ReadModel.Projecting.Configuration;
using CascadeEsdm.ReadModel.Projecting.Decorators;
using CascadeEsdm.SharedKernel.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.ReadModel.Composition;

public static class ReadModelBuilderExtensions
{
    public static ReadModelBuilder WithViews(this ReadModelBuilder builder, Action<ViewBuilder> viewConfiguration)
    {
        builder.Services.AddGeneric(typeof(IViewProjector<>), typeof(ViewProjector<>));
        builder.Services.AddGeneric(typeof(IViewEventMapper<>), typeof(ViewEventMapper<>));
        builder.Services.AddGeneric(typeof(IViewEventRegister<>), typeof(ViewEventRegister<>));
        builder.Services.AddGeneric(typeof(IEventCapabilityEvaluator<>), typeof(EventCapabilityEvaluator<>));
        builder.Services.AddGeneric(typeof(IViewCapabilityEvaluator<>), typeof(ViewCapabilityEvaluator<>));
        builder.Services.AddGeneric(typeof(IViewSequenceStore<>), typeof(SequenceStore<>));
        builder.Services.AddGeneric(typeof(IProjectionPartitionLocator<>), typeof(ProjectionPartitionLocator<>));

        builder.Services.AddGenericDecorator(typeof(IViewProjector<>), typeof(ProjectorNotificationDecorator<>));
        builder.Services.AddGenericDecorator(typeof(IViewProjector<>), typeof(ProjectorReplayDecorator<>));
        builder.Services.AddGenericDecorator(typeof(IViewProjector<>), typeof(ProjectorConcurrencyDecorator<>));
        builder.Services.AddGenericDecorator(typeof(IViewProjector<>), typeof(ProjectorTelemetryDecorator<>));

        var viewBuilder = new ViewBuilder(builder.Services);
        viewConfiguration(viewBuilder);

        var viewTypes = viewBuilder.Views;
        builder.Services.RegisterAutomapper(viewTypes);

        //TODO: Talk about Automapper converters, including those used to locate partitions.

        return builder;
    }

    private static void RegisterAutomapper(this IServiceCollection services, params Type[] viewTypes)
    {
        services.AddSingleton(sp => new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<GlobalViewProfile>();

            var genericProfileType = typeof(RootViewProfile<>);
            foreach (var viewType in viewTypes) {
                var viewProfileType = genericProfileType.MakeGenericType(viewType);
                var profile = ActivatorUtilities.CreateInstance(sp, viewProfileType);
                cfg.AddProfile((Profile)profile);
            }
        }));

        services.AddScoped(sp =>
        {
            var context = sp.GetRequiredService<MapperConfiguration>();
            return context.CreateMapper(sp.GetService);
        });
    }
}