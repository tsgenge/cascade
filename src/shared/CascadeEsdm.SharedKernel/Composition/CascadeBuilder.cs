using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class CascadeBuilder
{
    internal CascadeBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    internal IServiceCollection Services { get; }

    public ModelBuilder WithInfrastructure(Action<InfrastructureBuilder> configure)
    {
        var builder = new InfrastructureBuilder(Services);
        configure(builder);

        builder.Validate();

        return new ModelBuilder(Services, builder);
    }
}