using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class OnlyPartitioningEnvironment : AsbIntegrationEnvironmentBase
{
    protected override void ConfigureServices(IServiceCollection services, string azuriteConnectionString,
        string cosmosConnectionString, string serviceBusConnectionString)
    {
        services.AddCascadeEsdm(o =>
        {
            o.WithInfrastructure(i =>
                {
                    i.UsingCosmosDbStorage(cosmosConfig =>
                        {
                            cosmosConfig
                                .WithConnectionString(cosmosConnectionString)
                                .WithOptions(CreateEmulatorClientOptions())
                                .WithDatabaseName("cascade")
                                .WithEventStreamContainer<EventStreamContainer>();
                        })
                        .UsingOpenTelemetry()
                        .UsingAzureDistributedLocks(lb => { lb.WithConnectionString(azuriteConnectionString); })
                        .UsingAzureServiceBusReceiver("partitioned", r =>
                        {
                            r.WithConnectionString(serviceBusConnectionString);
                            r.WithTopic("partitioned-stream");
                            r.WithSubscription("partitioned-policies");
                        });
                })
                .WithWriteModel(b1 =>
                    b1
                        .UsingPolicies("partitioned", p => p
                            .AddPolicy<PartitionedPolicyOne>()
                            .AddPolicy<PartitionedPolicyTwo>()
                            .AddPolicy<PartitionedPolicyThree>())
                        .AddPolicyListener("partitioned")
                );
        });

        services.AddPolicyExecutionTracking();
    }
}
