# Local telemetry

OpenTelemetry is wired in `src/ChurchBulletin.ServiceDefaults/Extensions.cs`. The application starts with no telemetry setting.

## Local OTLP endpoint

Running `src/ChurchBulletin.AppHost` sets `OTEL_EXPORTER_OTLP_ENDPOINT`. `AppHost.cs` creates the distributed application and adds the `ui-server` and `worker` projects. It does not assign that variable itself. The Aspire AppHost sets it when the host runs, which is what the ServiceDefaults comment and the repository run notes already say.

A non-empty `OTEL_EXPORTER_OTLP_ENDPOINT` turns on the OTLP exporter (`UseOtlpExporter`). OpenTelemetry is configured for logging, metrics, and tracing, and that exporter sends them to the Aspire dashboard.

## Azure

In Azure the platform sets `APPLICATIONINSIGHTS_CONNECTION_STRING` and `OTEL_SERVICE_NAME`. A non-empty connection string turns on the Azure Monitor exporter, which sends traces, metrics and logs to Application Insights. After that exporter is added, the environment-variable resource detector runs again so `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` stay the service name the platform set.

Both exporters can be active together. With neither `OTEL_EXPORTER_OTLP_ENDPOINT` nor `APPLICATIONINSIGHTS_CONNECTION_STRING`, no exporter runs.
