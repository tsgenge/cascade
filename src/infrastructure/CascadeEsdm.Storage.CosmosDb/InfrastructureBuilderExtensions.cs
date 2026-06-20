using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.Storage.CosmosDb;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UsingCosmosStorage(
        this InfrastructureBuilder builder,
        Action<CosmosStorageBuilder> configure)
    {
        var storageBuilder = new CosmosStorageBuilder(builder);
        configure(storageBuilder);

        storageBuilder.Build();
        
        return builder;
    }
}
