using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class MixedPartitioningEnvironment : IntegrationEnvironment
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
                        .UsingAzureDistributedLocks(lb => { lb.WithConnectionString(azuriteConnectionString); })
                        .UsingAzureServiceBusReceiver(r =>
                        {
                            r.WithConnectionString(serviceBusConnectionString);
                            r.WithTopic("example-stream");
                            r.WithSubscription("test-policies");
                        })
                        .UsingAzureServiceBusReceiver("partitioned", r =>
                        {
                            r.WithConnectionString(serviceBusConnectionString);
                            r.WithTopic("partitioned-stream");
                            r.WithSubscription("partitioned-policies");
                        });
                })
                .WithWriteModel(b1 =>
                    b1
                        .UsingPolicies(p => p
                            .AddPolicy<SharedPolicyOne>()
                            .AddPolicy<SharedPolicyTwo>()
                            .AddPolicy<SharedPolicyThree>())
                        .UsingPolicies("partitioned", p => p
                            .AddPolicy<PartitionedPolicyOne>()
                            .AddPolicy<PartitionedPolicyTwo>()
                            .AddPolicy<PartitionedPolicyThree>())
                        .AddPolicyListener()
                        .AddPolicyListener("partitioned")
                );
        });

        services.AddPolicyExecutionTracking();
    }
}
