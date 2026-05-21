using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class SerialisationBuilder
{
    private readonly IServiceCollection _services;
    public SerialisationBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public void UseNewtonSoftSerialisation()
    {
        
    }    
}