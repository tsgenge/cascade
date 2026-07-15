using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class AllSharedPoliciesEnvironment : IntegrationEnvironment
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
                        .UsingAzureServiceBusReceiver(r =>
                        {
                            r.WithConnectionString(serviceBusConnectionString);
                            r.WithTopic("example-stream");
                            r.WithSubscription("test-policies");
                        })
                        .UsingAzureServiceBusReceiver("second-stream", r =>
                        {
                            r.WithConnectionString(serviceBusConnectionString);
                            r.WithTopic("second-stream");
                            r.WithSubscription("second-policies");
                        });
                })
                .WithWriteModel(b1 =>
                    b1
                        .UsingPolicies(p => p
                            .AddPolicy<SharedPolicyOne>()
                            .AddPolicy<SharedPolicyTwo>()
                            .AddPolicy<SharedPolicyThree>())
                        .UsingPolicies("second-stream", p => p
                            .AddPolicy<SharedPolicyOne>()
                            .AddPolicy<SharedPolicyTwo>()
                            .AddPolicy<SharedPolicyThree>())
                        .AddPolicyListener()
                        .AddPolicyListener("second-stream")
                );
        });

        services.AddPolicyExecutionTracking();
    }
}
