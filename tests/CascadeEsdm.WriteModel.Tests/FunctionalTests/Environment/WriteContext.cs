using CascadeEsdm.TestDomain.People.Commands;
using CascadeEsdm.TestDomain.People.Events;
using CascadeEsdm.TestDomain.People.Policies;
using CascadeEsdm.Testing;
using CascadeEsdm.WriteModel.CommandHandling;
using CascadeEsdm.WriteModel.Composition;
using CascadeEsdm.WriteModel.EventStream;
using CascadeEsdm.WriteModel.Tests.FunctionalTests.Decorators;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.WriteModel.Tests.FunctionalTests.Environment;

public class WriteContext : IntegrationEnvironment
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
                        .UsingAzureDistributedLocks(lb =>
                        {
                            lb.WithConnectionString(azuriteConnectionString);
                        })
                        .UsingAzureServiceBusReceiver(r =>
                        {
                            r.WithConnectionString(serviceBusConnectionString);
                            r.WithTopic("example-stream");
                            r.WithSubscription("test-policies");
                        });
                })
                .WithWriteModel(b1 =>
                    b1
                        .UsingExecutors(h => h
                            .AddCommandExecutor<AddPersonExecutor>()
                            .AddCommandExecutor<ChangePersonFirstNameExecutor>()
                            .AddCommandExecutor<RemovePersonExecutor>())
                        .UsingAppliers(h => h
                            .AddEventApplier<PersonAddedApplier>()
                            .AddEventApplier<PersonFirstNameChangedApplier>()
                            .AddEventApplier<PersonRemovedApplier>()
                            .AddEventApplier<SecurityDescriptorApplier>())
                        .UsingPolicies(p => p
                            .AddPolicy<PersonEatenRemovesPersonPolicy>())
                        .AddPolicyListener()
                );
        });
        services.AddSingleton(typeof(MessageChannel<>));
        services.AddGenericDecorator(typeof(ICommandHandler<>), typeof(MessageChannelHandler<>));
    }
}
