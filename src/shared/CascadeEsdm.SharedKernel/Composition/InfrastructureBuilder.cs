using CascadeEsdm.SharedKernel.Infrastructure.Concurrency;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using CascadeEsdm.SharedKernel.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CascadeEsdm.SharedKernel.Composition;

public class InfrastructureBuilder
{
    internal InfrastructureBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        
        Services.AddScoped<ITelemetryLogger, NoopTelemetryLogger>();
    }

    public IServiceCollection Services { get; }
}