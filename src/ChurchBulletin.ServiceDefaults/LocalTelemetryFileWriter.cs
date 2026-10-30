using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChurchBulletin.ServiceDefaults;

/// <summary>
/// Background service that writes telemetry data to local text files.
/// </summary>
public class LocalTelemetryFileWriter : BackgroundService, IAsyncDisposable
{
    internal const string MaxFileMegabytesKey = "LocalTelemetry:MaxFileMegabytes";
    internal const string RetainDaysKey = "LocalTelemetry:RetainDays";
    private const int DefaultMaxFileMegabytes = 100;
    private const int DefaultRetainDays = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private bool _disposed;

    public string TelemetryLogDirectory { get; }

    /// <summary>Size in bytes at which a telemetry file rolls to a new sequence file.</summary>
    internal long MaxFileBytes { get; }

    /// <summary>Number of days a telemetry file is kept after its last write.</summary>
    internal int RetainDays { get; }

    private readonly TimeProvider _timeProvider;
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;

    private string[] _openPaths = [];
    private JsonlFile? _traces;
    private JsonlFile? _events;
    private JsonlFile? _logs;
    private JsonlFile? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTelemetryFileWriter"/> class.
    /// </summary>
    public LocalTelemetryFileWriter(IConfiguration? configuration = null)
        : this(configuration, TimeProvider.System)
    {
    }

    internal LocalTelemetryFileWriter(IConfiguration? configuration, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = SampleAllDataAndRecorded,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };

        _meterListener = new MeterListener
        {
            InstrumentPublished = OnInstrumentPublished
        };
        _meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<float>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<decimal>(OnMeasurementRecorded);

        TelemetryLogDirectory = ResolveDirectory(configuration);
        MaxFileBytes = ReadPositiveInt(configuration, MaxFileMegabytesKey, DefaultMaxFileMegabytes) * 1024L * 1024L;
        RetainDays = ReadPositiveInt(configuration, RetainDaysKey, DefaultRetainDays);
    }

    private static string ResolveDirectory(IConfiguration? configuration)
    {
        var configuredPath = configuration?["LocalTelemetry:LogDirectory"];

        if (!string.IsNullOrEmpty(configuredPath))
        {
            return configuredPath;
        }

        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
        return Path.Combine(basePath, "logs");
    }

    private static int ReadPositiveInt(IConfiguration? configuration, string key, int fallback) =>
        int.TryParse(configuration?[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : fallback;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(TelemetryLogDirectory);

        var date = _timeProvider.GetUtcNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var tracesPath = Path.Combine(TelemetryLogDirectory, $"traces_{date}.jsonl");
        var eventsPath = Path.Combine(TelemetryLogDirectory, $"events_{date}.jsonl");
        var logsPath = Path.Combine(TelemetryLogDirectory, $"logs_{date}.jsonl");
        var metricsPath = Path.Combine(TelemetryLogDirectory, $"metrics_{date}.jsonl");
        _openPaths = [tracesPath, eventsPath, logsPath, metricsPath];

        CleanupOldFiles();

        _traces = new JsonlFile(tracesPath, MaxFileBytes, CleanupOldFiles);
        _events = new JsonlFile(eventsPath, MaxFileBytes, CleanupOldFiles);
        _logs = new JsonlFile(logsPath, MaxFileBytes, CleanupOldFiles);
        _metrics = new JsonlFile(metricsPath, MaxFileBytes, CleanupOldFiles);

        ActivitySource.AddActivityListener(_activityListener);
        _meterListener.Start();

        Trace.WriteLine($"Local telemetry file writer started. Writing to {Path.GetFullPath(TelemetryLogDirectory)}");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when stopping
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        _activityListener.Dispose();
        _meterListener.Dispose();

        _traces?.Dispose();
        _events?.Dispose();
        _logs?.Dispose();
        _metrics?.Dispose();

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public void WriteTraceEntry(Activity activity, string status)
    {
        if (_traces == null) return;

        WriteJsonLine(_traces, TraceEntryMapper.FromActivity(activity, status));
    }

    public void WriteEventEntry(Activity activity, ActivityEvent evt)
    {
        if (_events == null) return;

        WriteJsonLine(_events, new EventEntry(activity, evt));
    }

    public void WriteLogEntry(LogLevel level, string category, string message, Exception? exception = null)
    {
        if (_logs == null) return;

        WriteJsonLine(_logs, CreateLogEntry(level, category, message, exception));
    }

    private static LogEntry CreateLogEntry(LogLevel level, string category, string message, Exception? exception) =>
        new()
        {
            Timestamp = DateTime.UtcNow,
            Level = level.ToString(),
            Category = category,
            Message = message,
            Exception = exception == null ? null : new LogEntryError(exception)
        };

    private static void WriteJsonLine(JsonlFile file, object entry)
    {
        try
        {
            file.WriteLine(JsonSerializer.Serialize(entry, entry.GetType(), JsonOptions));
        }
        catch (Exception)
        {
            // Best-effort local telemetry file writes must never affect the application.
        }
    }

    public void WriteMetricEntry(string name, double value, string unit = "", IDictionary<string, object?>? tags = null)
    {
        if (_metrics == null) return;

        WriteJsonLine(_metrics, new MetricEntry(name, value, unit, tags));
    }

    private void CleanupOldFiles() =>
        TelemetryFileMaintenance.DeleteFilesOlderThan(TelemetryLogDirectory, RetainDays, _timeProvider, _openPaths);

    private void OnActivityStarted(Activity activity)
    {
        WriteTraceEntry(activity, "STARTED");
    }

    private static ActivitySamplingResult SampleAllDataAndRecorded(ref ActivityCreationOptions<ActivityContext> _) =>
        ActivitySamplingResult.AllDataAndRecorded;

    private void OnActivityStopped(Activity activity)
    {
        WriteTraceEntry(activity, "STOPPED");

        foreach (var evt in activity.Events)
        {
            WriteEventEntry(activity, evt);
        }
    }

    private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
    {
        // Listen to all instruments
        listener.EnableMeasurementEvents(instrument);
    }

    private void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) where T : struct
    {
        var tagsDictionary = new Dictionary<string, object?>();

        foreach (var tag in tags)
        {
            tagsDictionary[tag.Key] = tag.Value;
        }

        tagsDictionary["meter.name"] = instrument.Meter.Name;
        tagsDictionary["meter.version"] = instrument.Meter.Version;
        tagsDictionary["instrument.type"] = instrument.GetType().Name;

        var value = Convert.ToDouble(measurement);
        WriteMetricEntry(instrument.Name, value, instrument.Unit ?? "", tagsDictionary);
    }

    /// <summary>
    /// One append-only JSON-lines file that rolls to a numbered sequence file when it reaches its size cap.
    /// </summary>
    private sealed class JsonlFile(string path, long maxBytes, Action onRoll) : IDisposable
    {
        private readonly Lock _gate = new();
        private StreamWriter? _writer = Open(path);

        public void WriteLine(string line)
        {
            lock (_gate)
            {
                if (_writer is null) return;

                RollIfFull();
                _writer?.WriteLine(line);
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }

        private void RollIfFull()
        {
            if (_writer!.BaseStream.Length < maxBytes) return;

            _writer.Dispose();
            _writer = null;
            TelemetryFileMaintenance.TryMoveToNextSequence(path);
            onRoll();
            _writer = Open(path);
        }

        private static StreamWriter Open(string filePath) => new(filePath, append: true) { AutoFlush = true };
    }
}
