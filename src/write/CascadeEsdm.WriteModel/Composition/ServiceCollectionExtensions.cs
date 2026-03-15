using Autofac;
using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Exceptions;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CascadeEsdm.WriteModel.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCascadeWriteModel(this IServiceCollection services, WriteModelConfiguration configuration)
    {
        // var builder =  new CommandExecutorBuilder();
        // builderConfig(builder);

        services.AddScoped<ICommandAuthoriser, RoleBasedCommandAuthoriser>();

        services.AddGeneric(typeof(IAggregateFactory<>), typeof(AggregateFactory<>));
        services.AddGeneric(typeof(IEventApplierFactory<>), typeof(EventApplierFactory<>));
        services.AddScoped<IAggregatePartitionLocator, AggregatePartitionLocator>();
        services.AddGeneric(typeof(ICommandExecutorFactory<>), typeof(CommandExecutorFactory<>));
        
        //Register Event Stream Reader and Writer
        var eventReader = typeof(EventStreamReader<>).MakeGenericType(configuration.EventStreamContainerType);
        var eventWriter = typeof(EventStreamWriter<>).MakeGenericType(configuration.EventStreamContainerType);
        services.AddScoped(typeof(IEventStreamReader), eventReader);
        services.AddScoped(typeof(IEventStreamWriter), eventWriter);
        
        //Register event appliers
        
        //Register command handlers
        
        
        services.AddTransientGenericDecorator(typeof(ICommandHandler<,>), typeof(LoggingCommandHandlerDecorator<,>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<>), typeof(LoggingCommandHandlerDecorator<>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<,>), typeof(EventWritingCommandHandlerDecorator<,>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<>), typeof(EventWritingCommandHandlerDecorator<>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<,>), typeof(SerialisedCommandHandlerDecorator<,>));
        services.AddTransientGenericDecorator(typeof(ICommandHandler<>), typeof(SerialisedCommandHandlerDecorator<>));

        return services;
    }
    
    public static IServiceCollection RegisterAssemblyCommands(this IServiceCollection services, Assembly targetAssembly)
    {
        var assemblyTypes = targetAssembly.GetTypes();
        var aggregateTypes = assemblyTypes.Where(t => t.IsClass && !t.IsAbstract && typeof(IAggregateRoot).IsAssignableFrom(t));

        var missingExecutors = new List<Type>();
        foreach (var aggregateType in aggregateTypes)
        {
            var commandTypes = assemblyTypes.Where(t => !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t) && t.Namespace!.Contains(aggregateType.Namespace!));
            foreach (var commandType in commandTypes)
            {
                var handlerType = typeof(CommandHandler<,>).MakeGenericType(commandType, aggregateType);
                var interfaceType = typeof(ICommandHandler<>).MakeGenericType(commandType);
                
                var executorInterface = typeof(ICommandExecutor<,>).MakeGenericType(commandType, aggregateType);
                var executorType = assemblyTypes.FirstOrDefault(t => executorInterface.IsAssignableFrom(t));

                services.AddScoped(interfaceType, handlerType);
                if (executorType != null) {
                    services.AddScoped(executorInterface, executorType);
                }
                else {
                    missingExecutors.Add(executorInterface);
                }
            }
        }

        if (missingExecutors.Any()) {
            throw new MissingExecutorException(missingExecutors.Select(t => t.Name).ToArray());
        }

        return services;
    }

    public static ContainerBuilder RegisterAssemblyEventAppliers(this ContainerBuilder builder, Assembly targetAssembly)
    {
        builder.RegisterAssemblyTypes(targetAssembly)
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableToGenericType(typeof(IEventApplier<,>)))
            .AsImplementedInterfaces();
        return builder;
    }
}

public class WriteModelConfiguration
{
    public Type EventStreamContainerType { get; }

    public WriteModelConfiguration(Type eventStreamContainerType)
    {
        EventStreamContainerType = eventStreamContainerType ??
                                   throw new ArgumentNullException(nameof(eventStreamContainerType));
        if(!typeof(IDocumentContainerDefinition).IsAssignableFrom(eventStreamContainerType))
            throw new ArgumentException("The provided eventStreamContainerType does not derive from IDocumentContainerDefinition.");
    }
}

public class CommandExecutorBuilder()
{
    public CommandExecutorAssemblyBuilder RegisterFromTypeAssembly(Type exampleType)
    {
        return new CommandExecutorAssemblyBuilder(this);
    }
}

public class CommandExecutorAssemblyBuilder
{
    private readonly CommandExecutorBuilder _parent;

    public CommandExecutorAssemblyBuilder(CommandExecutorBuilder parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /*public CommandExecutorBuilder AllCommandExecutors()
    {
        
    }

    public CommandExecutorBuilder FilteredByNamespace(Type exampleType, bool includeChildrenNamespace = false)
    {
        
    }*/
}