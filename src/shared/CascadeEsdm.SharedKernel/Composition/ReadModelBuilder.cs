using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class ReadModelBuilder
{
    private readonly IServiceCollection _services;

    internal ReadModelBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services => _services;
}