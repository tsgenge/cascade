using CascadeEsdm.SharedKernel.Aggregates;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Composition;

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
        var aggregateTypes = assemblyTypes.Where(t =>
            t is { IsClass: true, IsAbstract: false } && typeof(IAggregateRoot).IsAssignableFrom(t));

        var missingExecutors = new List<Type>();
        foreach (var aggregateType in aggregateTypes) {
            var commandTypes = assemblyTypes.Where(t =>
                !t.IsAbstract && typeof(ICommand).IsAssignableFrom(t) &&
                t.Namespace!.Contains(aggregateType.Namespace!));
            foreach (var commandType in commandTypes) {
                var handlerType = typeof(CommandHandler<,>).MakeGenericType(commandType, aggregateType);
                var interfaceType = typeof(ICommandHandler<>).MakeGenericType(commandType);

                var executorInterface = typeof(ICommandExecutor<,>).MakeGenericType(commandType, aggregateType);
                var executorBaseInterface = typeof(ICommandExecutor<>).MakeGenericType(aggregateType);
                var executorType = assemblyTypes.FirstOrDefault(t => executorInterface.IsAssignableFrom(t));

                _services.AddScoped(interfaceType, handlerType);
                if (executorType != null) {
                    _services.AddScoped(executorInterface, executorType);
                    _services.AddScoped(executorBaseInterface, sp => sp.GetRequiredService(executorType));
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

    public CommandExecutorBuilder AddCommandExecutor<TExecutor>()
        where TExecutor : class
    {
        var executorType = typeof(TExecutor);
        var executorInterface = executorType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandExecutor<,>))
            ?? throw new InvalidOperationException($"{executorType.Name} does not implement ICommandExecutor<TCommand, TAggregate>.");

        var typeArgs = executorInterface.GetGenericArguments();
        var commandType = typeArgs[0];
        var aggregateType = typeArgs[1];

        var executorBaseInterface = typeof(ICommandExecutor<>).MakeGenericType(aggregateType);
        var handlerInterface = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handlerType = typeof(CommandHandler<,>).MakeGenericType(commandType, aggregateType);

        _services.AddScoped(executorInterface, executorType);
        _services.AddScoped(executorBaseInterface, sp => sp.GetRequiredService(executorInterface));
        _services.AddScoped(handlerInterface, handlerType);

        return this;
    }
}