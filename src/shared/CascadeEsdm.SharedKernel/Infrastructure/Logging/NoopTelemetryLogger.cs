using Microsoft.Extensions.Logging;

namespace CascadeEsdm.SharedKernel.Infrastructure.Logging;

public class NoopTelemetryLogger : ITelemetryLogger
{
    private readonly ILogger<NoopTelemetryLogger> _logger;

    public NoopTelemetryLogger(ILogger<NoopTelemetryLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDisposable StartOperation(string operationName, TelemetryParent? parent = null,
        TelemetryOperationKind kind = TelemetryOperationKind.Internal)
    {
        return new NoopDisposable();
    }

    public void AddMetric(string name, double value)
    {
        _logger.LogInformation("Not recording metric; implement a suitable ITelemetryLogger.");
    }

    public void AddCustomEvent(string name, IDictionary<string, string> properties)
    {
        _logger.LogInformation("Not recording custom event; implement a suitable ITelemetryLogger.");
    }

    public void TrackException(Exception exception)
    {
        _logger.LogError(exception, exception.Message);
    }
}