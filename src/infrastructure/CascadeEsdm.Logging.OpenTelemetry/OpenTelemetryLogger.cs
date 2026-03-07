using CascadeEsdm.SharedKernel.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CascadeEsdm.Logging.OpenTelemetry;

internal class OpenTelemetryLogger : ITelemetryLogger
{
    private const string SourceName = "Provider.Telemetry";
    private static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);
    private static readonly ConcurrentDictionary<string, Histogram<double>> Histograms = new(StringComparer.Ordinal);
    private readonly ILogger<OpenTelemetryLogger> _logger;

    public OpenTelemetryLogger(ILogger<OpenTelemetryLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddMetric(string name, double value)
    {
        var histogram = Histograms.GetOrAdd(name, n => Meter.CreateHistogram<double>(n));
        histogram.Record(value);
    }

    public void AddCustomEvent(string name, IDictionary<string, string> properties)
    {
        var activity = Activity.Current;
        if (activity != null) {
            var tags = new ActivityTagsCollection();
            foreach (var kvp in properties) {
                tags.Add(kvp.Key, kvp.Value);
            }

            activity.AddEvent(new ActivityEvent(name, tags: tags));
        }

        var scope = new List<KeyValuePair<string, object?>>(properties.Count + 1) { new("microsoft.custom_event.name", name) };

        foreach (var kvp in properties) {
            scope.Add(new KeyValuePair<string, object?>(kvp.Key, kvp.Value));
        }

        using (_logger.BeginScope(scope)) {
            _logger.LogInformation("{microsoft.custom_event.name}", name);
        }
    }

    public IDisposable StartOperation(string operationName, TelemetryParent? parent = null, TelemetryOperationKind kind = TelemetryOperationKind.Internal)
    {
        var activityKind = ConvertKind(kind);

        if (parent is null || !parent.IsValid) {
            var activity = ActivitySource.StartActivity(operationName, activityKind);
            return activity is null ? NoopDisposable.Instance : activity;
        }

        if (ActivityContext.TryParse(parent.TraceParent, parent.TraceState, out var parentContext)) {
            var activity = ActivitySource.StartActivity(operationName, activityKind, parentContext);
            return activity is null ? NoopDisposable.Instance : activity;
        }

        var fallbackActivity = ActivitySource.StartActivity(operationName, activityKind);
        return fallbackActivity is null ? NoopDisposable.Instance : fallbackActivity;
    }

    private static ActivityKind ConvertKind(TelemetryOperationKind kind)
    {
        return kind switch
        {
            TelemetryOperationKind.Server => ActivityKind.Server,
            TelemetryOperationKind.Client => ActivityKind.Client,
            TelemetryOperationKind.Producer => ActivityKind.Producer,
            TelemetryOperationKind.Consumer => ActivityKind.Consumer,
            _ => ActivityKind.Internal
        };
    }
}

public sealed class NoopDisposable : IDisposable
{
    public static readonly NoopDisposable Instance = new();
    public void Dispose() { }
}