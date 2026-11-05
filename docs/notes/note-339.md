# Note 339

`LiveTelemetryCounters` in `src/UI/Server/LiveTelemetry/LiveTelemetryCounters.cs` defines `private const double BytesPerMegabyte = 1024 * 1024`. The private method `ToWholeMegabytes` converts a byte count with `(long)Math.Round(bytes / BytesPerMegabyte, MidpointRounding.AwayFromZero)`.
