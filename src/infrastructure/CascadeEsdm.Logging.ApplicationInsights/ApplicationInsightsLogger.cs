using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;

namespace CascadeEsdm.Logging.ApplicationInsights;

public class ApplicationInsightsLogger : ITelemetryLogger
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsLogger(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public IDisposable StartOperation(string operationName, TelemetryParent? parent = null,
        TelemetryOperationKind kind = TelemetryOperationKind.Internal)
    {
        Activity? parentActivity = null;
        if (parent is { IsValid: true }) {
            parentActivity = new Activity(operationName);
            parentActivity.SetParentId(parent.TraceParent);
            if (!string.IsNullOrWhiteSpace(parent.TraceState))
                parentActivity.TraceStateString = parent.TraceState;
            parentActivity.Start();
        }

        IDisposable operation = IsRequest(kind)
            ? _telemetryClient.StartOperation<RequestTelemetry>(operationName)
            : _telemetryClient.StartOperation<DependencyTelemetry>(operationName);

        if (parentActivity is null)
            return operation;

        return new CompositeDisposable(operation, parentActivity);
    }

    public void AddMetric(string name, double value)
    {
        _telemetryClient.TrackMetric(name, value);
    }

    public void AddCustomEvent(string name, IDictionary<string, string> properties)
    {
        _telemetryClient.TrackEvent(name, properties);
    }

    public void TrackException(Exception exception)
    {
        _telemetryClient.TrackException(exception);
    }

    private bool IsRequest(TelemetryOperationKind kind)
    {
        return kind == TelemetryOperationKind.Server || kind == TelemetryOperationKind.Consumer;
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable _primary;
        private readonly IDisposable _secondary;

        internal CompositeDisposable(IDisposable primary, IDisposable secondary)
        {
            _primary = primary;
            _secondary = secondary;
        }

        public void Dispose()
        {
            _primary.Dispose();
            _secondary.Dispose();
        }
    }
}