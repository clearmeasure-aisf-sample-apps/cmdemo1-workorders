# Note 169

RequestClassifier.Classify returns RequestKind.FrontDoorProbe when the request header X-FD-HealthProbe equals "1" (`if (request.Headers[FrontDoorHealthProbeHeader] == "1")` then `return RequestKind.FrontDoorProbe;`, and `FrontDoorHealthProbeHeader` is `"X-FD-HealthProbe"`). That check runs before path classification. The method is `RequestClassifier.Classify` in `src/UI/Server/LiveTelemetry/RequestClassifier.cs`.
