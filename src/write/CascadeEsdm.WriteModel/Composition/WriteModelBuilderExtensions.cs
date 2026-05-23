using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CascadeEsdm.WriteModel.Composition;

public static class WriteModelBuilderExtensions
{
    public static WriteModelBuilder WithExecutors(this WriteModelBuilder builder, Action<CommandExecutorBuilder> commandExecutors)
    {
        var services = builder.Services;
        var eventStreamContainerType = builder.EventStreamContainerType;
        
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
        
        services.AddTransientGenericDecorator(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<>), typeof(LoggingCommandHandlerDecorator<>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<,>), typeof(EventWritingCommandHandlerDecorator<,>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<>), typeof(EventWritingCommandHandlerDecorator<>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<,>), typeof(SerialisedCommandHandlerDecorator<,>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<>), typeof(SerialisedCommandHandlerDecorator<>));

        var executorBuilder = new CommandExecutorBuilder(services);
        commandExecutors(executorBuilder);

        return builder;
    }
    
    public static WriteModelBuilder WithAppliers(this WriteModelBuilder builder, Action<EventApplierBuilder> eventAppliers)
    {
        var services = builder.Services;
        
        var applierBuilder = new EventApplierBuilder(services);
        eventAppliers(applierBuilder);

        return builder;
    }
}
