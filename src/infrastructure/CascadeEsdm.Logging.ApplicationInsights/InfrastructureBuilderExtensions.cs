using CascadeEsdm.Logging.ApplicationInsights;
using CascadeEsdm.SharedKernel.Composition;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureBuilderExtensions
{
    public static InfrastructureBuilder UsingApplicationInsights(this InfrastructureBuilder builder)
    {
        builder.Services.RemoveAll<ITelemetryLogger>();
        builder.Services.AddScoped<ITelemetryLogger, ApplicationInsightsLogger>();

        return builder;
    }
}