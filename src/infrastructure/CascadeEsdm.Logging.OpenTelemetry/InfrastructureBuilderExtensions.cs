using CascadeEsdm.Logging.OpenTelemetry;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UsingOpenTelemetry(this InfrastructureBuilder builder)
    {
        builder.Services.AddScoped<ITelemetryLogger, OpenTelemetryLogger>();

        return builder;
    }
}