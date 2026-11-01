# Note 228

`DependencyCallListener.OnHttpClientMeasurement` in `src/UI/Server/LiveTelemetry/DependencyCallListener.cs` returns without counting the outgoing HTTP call when `OpenTelemetry.Sdk.SuppressInstrumentation` is true (`if (OpenTelemetry.Sdk.SuppressInstrumentation) { return; }`).
