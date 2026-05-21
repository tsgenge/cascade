using CascadeEsdm.SharedKernel.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class CascadeBuilder
{
    internal IServiceCollection Services { get; }
    
    internal CascadeBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }
    
    public ModelBuilder WithInfrastructure(Action<InfrastructureBuilder> configure)
    {
        var builder = new InfrastructureBuilder(Services);
        configure(builder);

        builder.Validate();

        return new ModelBuilder(Services, builder);
    }

    public void WithSerialisationOnly(Action<SerialisationBuilder> configure)
    {
        
    }
}
