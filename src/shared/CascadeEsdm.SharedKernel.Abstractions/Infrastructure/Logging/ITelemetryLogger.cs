namespace CascadeEsdm.SharedKernel.Infrastructure.Logging;

public interface ITelemetryLogger
{
    IDisposable StartOperation(string operationName, TelemetryParent? parent = null, TelemetryOperationKind kind = TelemetryOperationKind.Internal);
    void AddMetric(string name, double value);
    void AddCustomEvent(string name, IDictionary<string, string> properties);
}

public enum TelemetryOperationKind
{
    Internal,
    Server,
    Client,
    Producer,
    Consumer
}

public record TelemetryParent(string TraceParent, string TraceState)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(TraceParent) && !string.IsNullOrWhiteSpace(TraceState);
}