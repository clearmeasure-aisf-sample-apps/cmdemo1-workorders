# Note 337

`LiveTelemetryCounters.LatencySampleCapacity` in `src/UI/Server/LiveTelemetry/LiveTelemetryCounters.cs` is the constant 4096 (`internal const int LatencySampleCapacity = 4096;`). The member summary states: "Latency samples kept per series; beyond this many in a minute the percentile uses the latest ones."
