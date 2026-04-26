using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.WriteModel;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using Microsoft.Extensions.DependencyInjection;

public class CommandExecutorBuilder
{
    private readonly IServiceCollection _services;
    
    public CommandExecutorBuilder(IServiceCollection services)
    {
        _services = services;
    }

    
    public CommandExecutorBuilder AddCommandsFromAssembly<TExampleType>()
    {
        var targetAssembly = typeof(TExampleType).Assembly;
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

                _services.AddScoped(interfaceType, handlerType);
                if (executorType != null) {
                    _services.AddScoped(executorInterface, executorType);
                }
                else {
                    missingExecutors.Add(executorInterface);
                }
            }
        }

        if (missingExecutors.Any()) {
            throw new MissingExecutorException(missingExecutors.Select(t => t.Name).ToArray());
        }

        return this;
    }

    public CommandExecutorBuilder AddCommandExecutor<TCommand, TAggregate, TExecutor>() 
        where TExecutor : class, ICommandExecutor<TCommand, TAggregate>
        where TCommand : ICommand
        where TAggregate : IAggregateRoot
    {
        _services.AddScoped<ICommandExecutor<TCommand, TAggregate>, TExecutor>();
        return this;
    }
}
