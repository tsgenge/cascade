# Logging in Cascade ESDM
Cascade ESDM uses an ITelemetryLogger interfsce for more advanced logging and tracing. To enable logging for automatic RequestOperations and enhanced exception logging you can select from OpenTelemetry (preferred modern standard) or Application Insights (legacy). 

Note choosing either of these (explicitly) will implicitly use ILogger; that means you won't get RequestOperations for things like policy execution.

## Enable OpenTelemetry logging

Add the package;

```
# OpenTelemetry
dotnet add package CascadeEsdm.Logging.OpenTelemetry
```

 Then link into your infrastructure definition using this;

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UsingOpenTelemetry()));
```

### Add OpenTelemetry!

Note this step just adds the required ITelemetryLogger implementation, it does not add OpenTelemetry to your application. Ensure you do this during your startup to add OpenTelemetry;

``` cscharp
services.AddOpenTelemetry();
```

## Enable Application Insights logging

Add the package;

```
# Application Insights
dotnet add package CascadeEsdm.Logging.ApplicationInsights
```

 Then link into your infrastructure definition using this;

```csharp
services.AddCascadeEsdm(cascade => cascade
    .WithInfrastructure(infra => infra
        .UsingApplicationInsights()));
```

### Add Application Insights!

Note this step just adds the required ITelemetryLogger implementation, it does not add Application Insights to your application. Ensure you do this during your startup to add Application Insights;

``` cscharp
services.AddApplicationInsightsTelemetry();
```

