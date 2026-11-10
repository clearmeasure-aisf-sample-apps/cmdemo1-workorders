using static ClearMeasure.Bootcamp.UI.Server.LiveTelemetry.RollingCounts;

namespace ClearMeasure.Bootcamp.UI.Server.LiveTelemetry;

/// <summary>
/// In-process counters over a rolling one-minute window: sixty one-second buckets of counts, and the latest
/// latency samples for percentiles; and the vitals of the process (CPU, memory, threads, requests executing).
/// Thread-safe; recording allocates nothing. Time comes from <see cref="TimeProvider"/>, so the window and the
/// CPU sampling can be driven in tests.
/// </summary>
public sealed class LiveTelemetryCounters
{
    // Length of the rolling window, in seconds.
    private const int WindowSeconds = 60;

    /// <summary>Latency samples kept per series; beyond this many in a minute the percentile uses the latest ones.</summary>
    internal const int LatencySampleCapacity = 4096;

    /// <summary>
    /// Shortest interval CPU use is measured over. A sample is taken when a request completes or the counts are
    /// read and the previous sample is at least this old: never on a timer, and not on every request.
    /// </summary>
    internal const int CpuSampleSeconds = 2;

    private const long WindowMilliseconds = WindowSeconds * 1000L;
    private const double BytesPerMegabyte = 1024 * 1024;

    private readonly TimeProvider _timeProvider;
    private readonly IProcessMeter _processMeter;
    private readonly DateTime _startedAt;
    private readonly long _startedSecond;
    private readonly Lock _gate = new();
    private readonly RollingCounts _counts = new(WindowSeconds);
    private readonly LatencySamples _requestLatency = new(LatencySampleCapacity);
    private readonly LatencySamples _sqlLatency = new(LatencySampleCapacity);
    private readonly CpuUsage _cpu;
    private int _inFlight;

    /// <summary>
    /// Starts counting at the current time of <paramref name="timeProvider"/>, for the current process.
    /// </summary>
    public LiveTelemetryCounters(TimeProvider timeProvider) : this(timeProvider, new SystemProcessMeter())
    {
    }

    /// <summary>
    /// Starts counting at the current time of <paramref name="timeProvider"/>, with the process figures of
    /// <paramref name="processMeter"/>.
    /// </summary>
    internal LiveTelemetryCounters(TimeProvider timeProvider, IProcessMeter processMeter)
    {
        _timeProvider = timeProvider;
        _processMeter = processMeter;
        var now = timeProvider.GetUtcNow().UtcTicks;
        _startedAt = new DateTime(now - now % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        _startedSecond = NowMilliseconds() / 1000;
        _cpu = new CpuUsage(NowMilliseconds(), processMeter.CpuTime, processMeter.ProcessorCount, CpuSampleSeconds * 1000L);
    }

    /// <summary>
    /// Counts a request as executing, until <see cref="RequestEnded"/>.
    /// </summary>
    public void RequestStarted() => Interlocked.Increment(ref _inFlight);

    /// <summary>
    /// Stops counting a request as executing.
    /// </summary>
    public void RequestEnded() => Interlocked.Decrement(ref _inFlight);

    /// <summary>
    /// Records a completed incoming request. Traffic durations feed the request percentile; pass null for
    /// long-lived requests (WebSockets) whose duration is not a response time.
    /// </summary>
    public void RecordRequest(RequestKind kind, int statusCode, TimeSpan? elapsed)
    {
        var nowMs = NowMilliseconds();
        lock (_gate)
        {
            SampleCpuWhenDue(nowMs);
            if (!CountTraffic(nowMs, kind))
            {
                return;
            }

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _counts.Increment(nowMs, Counter.TrafficErrors);
            }

            if (elapsed is { } duration)
            {
                _requestLatency.Add(nowMs, ToWholeMilliseconds(duration));
            }
        }
    }

    /// <summary>Counts the request by kind; true when it is traffic (probes have no errors or latency).</summary>
    private bool CountTraffic(long nowMs, RequestKind kind)
    {
        var counter = kind switch
        {
            RequestKind.FrontDoorProbe => Counter.FrontDoorProbes,
            RequestKind.Probe => Counter.Probes,
            RequestKind.FrontDoorTraffic => Counter.FrontDoorTraffic,
            _ => Counter.DirectTraffic
        };
        _counts.Increment(nowMs, counter);
        return counter is Counter.FrontDoorTraffic or Counter.DirectTraffic;
    }

    /// <summary>
    /// Records a completed SQL command and its duration. <paramref name="duringRequest"/> tells a command executed
    /// while an HTTP request was being handled (traffic or probe) from one the process executed on its own.
    /// </summary>
    public void RecordSqlCommand(TimeSpan elapsed, bool duringRequest)
    {
        var nowMs = NowMilliseconds();
        lock (_gate)
        {
            _counts.Increment(nowMs, Counter.SqlCommands);
            if (duringRequest)
            {
                _counts.Increment(nowMs, Counter.SqlCommandsDuringRequests);
            }

            _sqlLatency.Add(nowMs, ToWholeMilliseconds(elapsed));
        }
    }

    /// <summary>Records a completed outgoing HTTP call.</summary>
    public void RecordHttpClientCall()
    {
        var nowMs = NowMilliseconds();
        lock (_gate)
        {
            _counts.Increment(nowMs, Counter.HttpClientCalls);
        }
    }

    /// <summary>
    /// Records a request that ended in an unhandled exception: one that escaped the pipeline, or one the
    /// exception handler turned into an error response. Traffic and probes alike; a request its client aborted
    /// is not one.
    /// </summary>
    public void RecordUnhandledException()
    {
        var nowMs = NowMilliseconds();
        lock (_gate)
        {
            _counts.Increment(nowMs, Counter.UnhandledExceptions);
        }
    }

    /// <summary>Returns the counts of the last 60 seconds and the vitals of the process now.</summary>
    public LiveTelemetrySnapshot Snapshot()
    {
        var nowMs = NowMilliseconds();
        var nowSecond = nowMs / 1000;
        CounterTotals totals;
        int? requestP95;
        int? sqlP95;
        double cpuPercent;
        lock (_gate)
        {
            SampleCpuWhenDue(nowMs);
            cpuPercent = _cpu.Percent;
            totals = _counts.Totals(nowSecond);
            requestP95 = _requestLatency.Percentile95(nowMs - WindowMilliseconds);
            sqlP95 = _sqlLatency.Percentile95(nowMs - WindowMilliseconds);
        }

        return new LiveTelemetrySnapshot(
            WindowSeconds,
            _startedAt,
            totals.Requests(requestP95),
            totals.Probes(),
            totals.Sql(sqlP95),
            totals.HttpClient(),
            Vitals(cpuPercent, totals.UnhandledExceptions, nowSecond));
    }

    private ProcessVitals Vitals(double cpuPercent, int unhandledExceptions, long nowSecond) =>
        new(cpuPercent,
            ToWholeMegabytes(_processMeter.WorkingSetBytes),
            ToWholeMegabytes(_processMeter.GcHeapBytes),
            _processMeter.ThreadPoolThreads,
            Volatile.Read(ref _inFlight),
            unhandledExceptions,
            Math.Max(0, nowSecond - _startedSecond));

    private long NowMilliseconds() => _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

    // The lock is held. Reading the processor time is one system call, made at most once per sampling interval.
    private void SampleCpuWhenDue(long nowMs)
    {
        if (_cpu.IsDue(nowMs))
        {
            _cpu.Sample(nowMs, _processMeter.CpuTime);
        }
    }

    private static int ToWholeMilliseconds(TimeSpan elapsed) =>
        (int)Math.Clamp(Math.Round(elapsed.TotalMilliseconds, MidpointRounding.AwayFromZero), 0, int.MaxValue);

    private static long ToWholeMegabytes(long bytes) =>
        (long)Math.Round(bytes / BytesPerMegabyte, MidpointRounding.AwayFromZero);
}
