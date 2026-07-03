using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Policies;
using CascadeEsdm.WriteModel.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Composition;

public static class WriteModelBuilderExtensions
{
    public static WriteModelBuilder UsingExecutors(this WriteModelBuilder builder,
        Action<CommandExecutorBuilder> commandExecutors)
    {
        var services = builder.Services;

        // We can't get here without calling WithInfrastructure.
        var eventStreamContainerType =
            services.First(s => s.ServiceType == typeof(IEventStreamContainer)).ImplementationType!;

        var executorBuilder = new CommandExecutorBuilder(services);
        commandExecutors(executorBuilder);

        services.AddScoped<ICommandAuthoriser, RoleBasedCommandAuthoriser>();

        services.AddGeneric(typeof(IAggregateHydrator<>), typeof(AggregateHydrator<>));
        services.AddGeneric(typeof(IAggregateFactory<>), typeof(AggregateFactory<>));
        services.AddGeneric(typeof(IEventApplierFactory<>), typeof(EventApplierFactory<>));
        services.AddScoped<IAggregatePartitionLocator, AggregatePartitionLocator>();
        services.AddGeneric(typeof(ICommandExecutorFactory<>), typeof(CommandExecutorFactory<>));
        services.AddGeneric(typeof(ISnapshotReader<>), typeof(SnapshotReader<>));

        var eventReader = typeof(EventStreamReader<>).MakeGenericType(eventStreamContainerType);
        var eventWriter = typeof(EventStreamWriter<>).MakeGenericType(eventStreamContainerType);
        services.AddScoped(typeof(IEventStreamReader), eventReader);
        services.AddScoped(typeof(IEventStreamWriter), eventWriter);

        services.AddGenericDecorator(typeof(ICommandHandler<>), typeof(LoggingCommandHandlerDecorator<>));
        //services.AddGenericDecorator(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));
        services.AddGenericDecorator(typeof(ICommandHandler<>), typeof(EventWritingCommandHandlerDecorator<>));
        //services.AddGenericDecorator(typeof(ICommandHandler<,>), typeof(EventWritingCommandHandlerDecorator<,>));        
        services.AddGenericDecorator(typeof(ICommandHandler<>), typeof(SerialisedCommandHandlerDecorator<>));
        //services.AddGenericDecorator(typeof(ICommandHandler<,>), typeof(SerialisedCommandHandlerDecorator<,>));        

        return builder;
    }

    public static WriteModelBuilder UsingAppliers(this WriteModelBuilder builder,
        Action<EventApplierBuilder> eventAppliers)
    {
        var services = builder.Services;

        var applierBuilder = new EventApplierBuilder(services);
        eventAppliers(applierBuilder);

        return builder;
    }

    public static WriteModelBuilder UsingPolicies(this WriteModelBuilder builder,
        Action<PolicyBuilder> policies)
    {
        var services = builder.Services;

        var policyBuilder = new PolicyBuilder(services);
        policies(policyBuilder);

        services.AddScoped<IPolicyDispatcher, PolicyDispatcher>();

        return builder;
    }

    public static WriteModelBuilder UsingPolicyListener(this WriteModelBuilder builder,
        Action<PolicyListenerBuilder>? configure = null)
    {
        return builder.AddPolicyListener(null, configure);
    }

    public static WriteModelBuilder AddPolicyListener(this WriteModelBuilder builder,
        string? name = null, Action<PolicyListenerBuilder>? configure = null)
    {
        var services = builder.Services;

        var listenerBuilder = new PolicyListenerBuilder(services, name);
        configure?.Invoke(listenerBuilder);
        listenerBuilder.Build();

        return builder;
    }
}