using CascadeEsdm.Testing;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.CommandHandlers;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Commands;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Decorators;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

internal static class PolicyTestServiceRegistration
{
    public static void AddPolicyExecutionTracking(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<SharedPolicyOneExecuted>,
            PolicyExecutedCommandHandler<SharedPolicyOneExecuted>>();
        services.AddScoped<ICommandHandler<SharedPolicyTwoExecuted>,
            PolicyExecutedCommandHandler<SharedPolicyTwoExecuted>>();
        services.AddScoped<ICommandHandler<SharedPolicyThreeExecuted>,
            PolicyExecutedCommandHandler<SharedPolicyThreeExecuted>>();
        services.AddScoped<ICommandHandler<PartitionedPolicyOneExecuted>,
            PolicyExecutedCommandHandler<PartitionedPolicyOneExecuted>>();
        services.AddScoped<ICommandHandler<PartitionedPolicyTwoExecuted>,
            PolicyExecutedCommandHandler<PartitionedPolicyTwoExecuted>>();
        services.AddScoped<ICommandHandler<PartitionedPolicyThreeExecuted>,
            PolicyExecutedCommandHandler<PartitionedPolicyThreeExecuted>>();

        services.AddSingleton(typeof(MessageChannel<>));
        services.AddGenericDecorator(typeof(ICommandHandler<>), typeof(MessageChannelHandler<>));
    }
}
