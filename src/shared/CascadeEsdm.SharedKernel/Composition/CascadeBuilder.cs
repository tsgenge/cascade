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
    
    public ModelSelector WithInfrastructure(Action<InfrastructureBuilder> configure)
    {
        var builder = new InfrastructureBuilder(Services);
        configure(builder);

        builder.Validate();
        
        return new ModelSelector(Services, builder);
    }
}
