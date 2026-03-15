using Autofac;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Hydration;
using CascadeEsdm.WriteModel.Security;

namespace CascadeEsdm.WriteModel.Composition;

public class WriteModelComposer : Module
{
    private readonly Type _eventStreamContainerType;

    public WriteModelComposer(Type eventStreamContainerType)
    {
        _eventStreamContainerType = eventStreamContainerType ?? throw new ArgumentNullException(nameof(eventStreamContainerType));
        if (!typeof(IDocumentContainerDefinition).IsAssignableFrom(_eventStreamContainerType))
            throw new ArgumentException("The provided event stream container type must inherit from IDocumentContainerDefinition", nameof(eventStreamContainerType));
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterGeneric(typeof(AggregateHydrator<>))
            .AsImplementedInterfaces()
            .IfNotRegistered(typeof(IAggregateHydrator<>));
        
        var readerType = typeof(EventStreamReader<>).MakeGenericType(_eventStreamContainerType);
        var writerType = typeof(EventStreamWriter<>).MakeGenericType(_eventStreamContainerType);
        
        builder.RegisterTypes(
                typeof(RoleBasedCommandAuthoriser),
                readerType,
                writerType
            )
            .AsImplementedInterfaces();
        
        builder.RegisterGeneric(typeof(AggregateFactory<>))
            .AsImplementedInterfaces();
        
        builder.RegisterGeneric(typeof(EventApplierFactory<>))
            .AsImplementedInterfaces();
        
        builder.RegisterTypes(typeof(AggregatePartitionLocator))
            .AsImplementedInterfaces();
        
        builder.RegisterGeneric(typeof(CommandExecutorFactory<>))
            .AsImplementedInterfaces();

        builder.RegisterGeneric(typeof(CommandHandler<,>))
            .AsImplementedInterfaces();
        
        builder.RegisterGenericDecorator(typeof(LoggingCommandHandlerDecorator<,>), typeof(ICommandHandler<,>));
        builder.RegisterGenericDecorator(typeof(LoggingCommandHandlerDecorator<>), typeof(ICommandHandler<>));
        builder.RegisterGenericDecorator(typeof(EventWritingCommandHandlerDecorator<,>), typeof(ICommandHandler<,>));
        builder.RegisterGenericDecorator(typeof(EventWritingCommandHandlerDecorator<>), typeof(ICommandHandler<>));
        builder.RegisterGenericDecorator(typeof(SerialisedCommandHandlerDecorator<,>), typeof(ICommandHandler<,>));
        builder.RegisterGenericDecorator(typeof(SerialisedCommandHandlerDecorator<>), typeof(ICommandHandler<>));        
    }
}