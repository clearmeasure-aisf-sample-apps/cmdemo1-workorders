namespace ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

/// <summary>
/// CPU use between two samples as a percentage of all processors available to the process; not thread-safe
/// on its own (the owner holds the lock). Zero until the first interval has passed.
/// </summary>
internal sealed class CpuUsage(long startedMs, TimeSpan cpuTime, int processorCount, long sampleMilliseconds)
{
    private readonly int _processorCount = Math.Max(1, processorCount);
    private long _sampledAtMs = startedMs;
    private TimeSpan _cpuTime = cpuTime;

    /// <summary>Percentage over the last sampled interval: 0 to 100, one decimal.</summary>
    public double Percent { get; private set; }

    /// <summary>True when the previous sample is a full interval old, or the clock went back.</summary>
    public bool IsDue(long nowMs) => nowMs < _sampledAtMs || nowMs - _sampledAtMs >= sampleMilliseconds;

    public void Sample(long nowMs, TimeSpan cpuTime)
    {
        var wallMs = nowMs - _sampledAtMs;
        if (wallMs > 0)
        {
            var usedMs = (cpuTime - _cpuTime).TotalMilliseconds;
            var percent = 100.0 * usedMs / (wallMs * (double)_processorCount);
            Percent = Math.Round(Math.Clamp(percent, 0, 100), 1, MidpointRounding.AwayFromZero);
        }

        _sampledAtMs = nowMs;
        _cpuTime = cpuTime;
    }
}
