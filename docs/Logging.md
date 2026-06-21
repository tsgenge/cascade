# Logging in Cascade ESDM
Cascade ESDM uses OpenTelemetry for logging and tracing. To enable logging add the `CascadeEsdm.Logging.OpenTelemetry` to your solution, and enable during composition of the infratructure;

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UsingOpenTelemetry()));
```

## Add OpenTelemetry!

Note this step just adds the required ITelemetryLogger implementation, it does not add OpenTelemetry to your application. Ensure you do this during your startup to add OpenTelemetry;

``` cscharp
services.AddOpenTelemetry();
```

