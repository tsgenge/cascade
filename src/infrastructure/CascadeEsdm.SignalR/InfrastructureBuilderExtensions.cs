using CascadeEsdm.SharedKernel.Composition;

namespace CascadeEsdm.SignalR;

public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UseCosmosDbStorage(
        this InfrastructureBuilder builder,
        Action<SignalRBuilder> configure)
    {
        var storageBuilder = new SignalRBuilder(builder);
        configure(storageBuilder);

        storageBuilder.Build();

        return builder;
    }
}