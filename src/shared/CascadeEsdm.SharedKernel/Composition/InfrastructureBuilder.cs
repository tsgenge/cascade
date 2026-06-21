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
    }

    public IServiceCollection Services { get; }

    internal bool HasStorage()
    {
        return Services.Any(s => s.ServiceType == typeof(IPartitionedContainer<>));
    }

    internal bool HasLocking()
    {
        return Services.Any(s => s.ServiceType == typeof(IDistributedLockProvider));
    }

    internal bool HasLogging()
    {
        return Services.Any(s => s.ServiceType == typeof(ITelemetryLogger));
    }

    internal bool HasContainer()
    {
        return Services.Any(s => s.ServiceType != typeof(IEventStreamContainer));
    }

    internal void Validate()
    {
        var missing = new List<string>();

        if (!HasStorage())
            missing.Add("Storage Provider");
        if (!HasLocking())
            missing.Add("Distributed Lock Provider, such as CascadeEsdm.DistributedLocks");
        if (!HasLogging())
            missing.Add("Telemetry Logger, such as CascadeEsdm.Logging.OpenTelemetry");

        if (!HasContainer())
            missing.Add("Event Stream Container is required, use WithEventStreamContainer()");

        if (missing.Any()) {
            throw new InvalidOperationException(
                $"Missing required infrastructure components: {string.Join(", ", missing)}. " +
                "Ensure you have called the appropriate Use* methods on the infrastructure builder.");
        }
    }
}