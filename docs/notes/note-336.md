# Note 336

`LiveTelemetryCounters.CpuSampleSeconds` in `src/UI/Server/LiveTelemetry/LiveTelemetryCounters.cs` is the constant 2 (`internal const int CpuSampleSeconds = 2;`). The member summary states: "Shortest interval CPU use is measured over. A sample is taken when a request completes or the counts are read and the previous sample is at least this old: never on a timer, and not on every request."
