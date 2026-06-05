using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.Storage.Azure;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class AzureTableStorageBuilderExtensions
{
    public static InfrastructureBuilder UseAzureTableStorage(
        this InfrastructureBuilder builder,
        Action<AzureTableStorageBuilder> configure)
    {
        var storageBuilder = new AzureTableStorageBuilder(builder);
        configure(storageBuilder);

        storageBuilder.Build();

        return builder;
    }
}
