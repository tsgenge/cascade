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

public record TelemetryParent
{
    public string TraceParent { get; }
    public string TraceState { get; }

    public TelemetryParent(string traceParent, string traceState)
    {
        TraceParent = traceParent;
        TraceState = traceState;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(TraceParent) && !string.IsNullOrWhiteSpace(TraceState);
}