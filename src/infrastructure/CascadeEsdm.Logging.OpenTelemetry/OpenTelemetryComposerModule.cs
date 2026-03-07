using Autofac;
using CascadeEsdm.SharedKernel.Infrastructure.Logging;

namespace CascadeEsdm.Logging.OpenTelemetry;

public class OpenTelemetryComposerModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType(typeof(OpenTelemetryLogger))
            .AsImplementedInterfaces()
            .IfNotRegistered(typeof(ITelemetryLogger));
    }
}