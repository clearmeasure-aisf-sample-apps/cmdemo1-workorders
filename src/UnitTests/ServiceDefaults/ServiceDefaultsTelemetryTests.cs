using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ChurchBulletin.ServiceDefaults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class LocalTelemetryFileWriterTests
{
    private const string FixedDate = "2026-01-15";
    private static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private const long OneMegabyte = 1024L * 1024L;

    [Test]
    public void Constructor_WithoutSettings_ShouldUseDefaults()
    {
        var writer = new LocalTelemetryFileWriter(new StubConfiguration(CreateTempDirectory()));

        writer.MaxFileBytes.ShouldBe(100 * OneMegabyte);
        writer.RetainDays.ShouldBe(3);
    }

    [Test]
    public void Constructor_WithSettings_ShouldReadThem()
    {
        var writer = new LocalTelemetryFileWriter(CreateConfiguration(CreateTempDirectory(), "5", "7"));

        writer.MaxFileBytes.ShouldBe(5 * OneMegabyte);
        writer.RetainDays.ShouldBe(7);
    }

    [TestCase("abc")]
    [TestCase("0")]
    [TestCase("-2")]
    public void Constructor_WithInvalidSettings_ShouldFallBackToDefaults(string setting)
    {
        var writer = new LocalTelemetryFileWriter(CreateConfiguration(CreateTempDirectory(), setting, setting));

        writer.MaxFileBytes.ShouldBe(100 * OneMegabyte);
        writer.RetainDays.ShouldBe(3);
    }

    [Test]
    public void ShouldUseConfiguredDirectory_WhenConfigurationProvided()
    {
        var directory = CreateTempDirectory();
        var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));

        writer.TelemetryLogDirectory.ShouldBe(directory);
    }

    [Test]
    public async Task WriteLogEntry_PastOneMegabyteCap_ShouldRollToSecondFile()
    {
        var directory = CreateTempDirectory();
        await using var writer = CreateFixedClockWriter(directory, "1");
        using var cts = new CancellationTokenSource();
        await StartAsync(writer, directory, cts.Token);

        var written = WriteMessagesPast(writer, OneMegabyte * 3 / 2);
        await StopAsync(writer, cts);

        LogFileNames(directory).ShouldBe([$"logs_{FixedDate}.jsonl", $"logs_{FixedDate}_0.jsonl"], ignoreOrder: true);
        var lines = ReadLines(directory, $"logs_{FixedDate}_0.jsonl").Concat(ReadLines(directory, $"logs_{FixedDate}.jsonl")).ToList();
        var longestLineBytes = lines.Max(line => Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length);
        foreach (var file in Directory.GetFiles(directory, "logs_*.jsonl"))
        {
            new FileInfo(file).Length.ShouldBeLessThanOrEqualTo(OneMegabyte + longestLineBytes);
        }

        lines.Select(MessageOf).ShouldBe(written);
    }

    [Test]
    public async Task WriteLogEntry_PastCapTwice_ShouldRollInOrder()
    {
        var directory = CreateTempDirectory();
        await using var writer = CreateFixedClockWriter(directory, "1");
        using var cts = new CancellationTokenSource();
        await StartAsync(writer, directory, cts.Token);

        var written = WriteMessagesPast(writer, OneMegabyte * 5 / 2);
        await StopAsync(writer, cts);

        LogFileNames(directory).Length.ShouldBe(3);
        var first = ReadLines(directory, $"logs_{FixedDate}_0.jsonl").Select(MessageOf).ToList();
        var second = ReadLines(directory, $"logs_{FixedDate}_1.jsonl").Select(MessageOf).ToList();
        var live = ReadLines(directory, $"logs_{FixedDate}.jsonl").Select(MessageOf).ToList();
        first[0].ShouldBe(written[0]);
        first.Concat(second).Concat(live).ShouldBe(written);
        live[^1].ShouldBe(written[^1]);
    }

    [Test]
    public void NextSequencePath_WithExistingSequences_ShouldUseHighestPlusOne()
    {
        var directory = CreateTempDirectory();
        var basePath = Path.Combine(directory, "logs_d.jsonl");
        File.WriteAllText(Path.Combine(directory, "logs_d_0.jsonl"), "{}");
        File.WriteAllText(Path.Combine(directory, "logs_d_3.jsonl"), "{}");
        File.WriteAllText(Path.Combine(directory, "logs_d_x.jsonl"), "{}");
        File.WriteAllText(Path.Combine(directory, "metrics_d_9.jsonl"), "{}");

        TelemetryFileMaintenance.NextSequencePath(basePath).ShouldBe(Path.Combine(directory, "logs_d_4.jsonl"));
    }

    [Test]
    public void NextSequencePath_WithoutSequences_ShouldStartAtZero()
    {
        var directory = CreateTempDirectory();
        var basePath = Path.Combine(directory, "logs_d.jsonl");

        TelemetryFileMaintenance.NextSequencePath(basePath).ShouldBe(Path.Combine(directory, "logs_d_0.jsonl"));
    }

    [Test]
    public void TryMoveToNextSequence_WhenFileMissing_ShouldNotThrow()
    {
        var basePath = Path.Combine(CreateTempDirectory(), "missing", "logs_d.jsonl");

        Should.NotThrow(() => TelemetryFileMaintenance.TryMoveToNextSequence(basePath));
    }

    [Test]
    public async Task ExecuteAsync_WithOldAndNewFiles_ShouldDeleteOnlyOldAtStart()
    {
        var directory = CreateTempDirectory();
        var oldFile = CreateJsonl(directory, "logs_2026-01-10.jsonl", new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc));
        var newFile = CreateJsonl(directory, "logs_2026-01-13.jsonl", new DateTime(2026, 1, 14, 0, 0, 0, DateTimeKind.Utc));
        await using var writer = CreateFixedClockWriter(directory, null);
        using var cts = new CancellationTokenSource();

        await StartAsync(writer, directory, cts.Token);
        await StopAsync(writer, cts);

        File.Exists(oldFile).ShouldBeFalse();
        File.Exists(newFile).ShouldBeTrue();
    }

    [Test]
    public async Task WriteLogEntry_WhenRolling_ShouldApplyRetention()
    {
        var directory = CreateTempDirectory();
        await using var writer = CreateFixedClockWriter(directory, "1");
        using var cts = new CancellationTokenSource();
        await StartAsync(writer, directory, cts.Token);
        var oldFile = CreateJsonl(directory, "events_2026-01-01.jsonl", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        WriteMessagesPast(writer, OneMegabyte * 3 / 2);
        await StopAsync(writer, cts);

        File.Exists(oldFile).ShouldBeFalse();
    }

    [Test]
    public void DeleteFilesOlderThan_ShouldDeleteOldKeepNewAndKeepPaths()
    {
        var directory = CreateTempDirectory();
        var old = FixedNow.UtcDateTime.AddDays(-10);
        var oldFile = CreateJsonl(directory, "logs_2026-01-05.jsonl", old);
        var recentFile = CreateJsonl(directory, "logs_2026-01-14.jsonl", FixedNow.UtcDateTime.AddDays(-1));
        var openFile = CreateJsonl(directory, "metrics_2026-01-05.jsonl", old);
        var textFile = CreateJsonl(directory, "notes.txt", old);

        TelemetryFileMaintenance.DeleteFilesOlderThan(directory, 3, new StubTimeProvider(FixedNow), [openFile]);

        File.Exists(oldFile).ShouldBeFalse();
        File.Exists(recentFile).ShouldBeTrue();
        File.Exists(openFile).ShouldBeTrue();
        File.Exists(textFile).ShouldBeTrue();
    }

    [Test]
    public void DeleteFilesOlderThan_WhenDirectoryMissing_ShouldNotThrow()
    {
        var directory = Path.Combine(CreateTempDirectory(), "missing");

        Should.NotThrow(() =>
            TelemetryFileMaintenance.DeleteFilesOlderThan(directory, 3, new StubTimeProvider(FixedNow), []));
    }

    [Test]
    public async Task ExecuteAsync_ShouldCreateLogFiles_ThenCancelCleanly()
    {
        var directory = CreateTempDirectory();
        await using var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));

        using var cts = new CancellationTokenSource();
        var start = writer.StartAsync(cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && Directory.GetFiles(directory, "*.jsonl").Length == 0)
        {
            await Task.Delay(25, CancellationToken.None);
        }

        Directory.Exists(directory).ShouldBeTrue();
        Directory.GetFiles(directory, "*.jsonl").Length.ShouldBeGreaterThanOrEqualTo(1);

        writer.WriteLogEntry(LogLevel.Error, "cat", "msg", new InvalidOperationException("write-me"));

        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllDataAndRecorded;
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("telemetry-test");
        using (var activity = source.CreateActivity("sample", ActivityKind.Internal)?.Start())
        {
            activity.ShouldNotBeNull();
            activity.AddEvent(new ActivityEvent("evt"));
            writer.WriteTraceEntry(activity, "STARTED");
            writer.WriteEventEntry(activity, activity.Events.First());
        }

        writer.WriteMetricEntry("m1", 1.5, "ms", new Dictionary<string, object?> { ["k"] = "v" });

        await cts.CancelAsync();
        await writer.StopAsync(CancellationToken.None);
        await start;

        var logsFile = Directory.GetFiles(directory, "logs_*.jsonl").Single();
        var lengthAfterStop = new FileInfo(logsFile).Length;
        Should.NotThrow(() => writer.WriteLogEntry(LogLevel.Error, "cat", "after-stop"));
        new FileInfo(logsFile).Length.ShouldBe(lengthAfterStop);
    }

    private static LocalTelemetryFileWriter CreateFixedClockWriter(string directory, string? maxFileMegabytes) =>
        new(CreateConfiguration(directory, maxFileMegabytes, null), new StubTimeProvider(FixedNow));

    private static StubConfiguration CreateConfiguration(string directory, string? maxFileMegabytes, string? retainDays) =>
        new(directory, new Dictionary<string, string?>
        {
            [LocalTelemetryFileWriter.MaxFileMegabytesKey] = maxFileMegabytes,
            [LocalTelemetryFileWriter.RetainDaysKey] = retainDays
        });

    private static async Task StartAsync(LocalTelemetryFileWriter writer, string directory, CancellationToken token)
    {
        await writer.StartAsync(token);

        var liveLogs = Path.Combine(directory, $"logs_{FixedDate}.jsonl");
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && !File.Exists(liveLogs))
        {
            await Task.Delay(25, CancellationToken.None);
        }

        File.Exists(liveLogs).ShouldBeTrue();
    }

    private static async Task StopAsync(LocalTelemetryFileWriter writer, CancellationTokenSource cts)
    {
        await cts.CancelAsync();
        await writer.StopAsync(CancellationToken.None);
    }

    private static List<string> WriteMessagesPast(LocalTelemetryFileWriter writer, long totalBytes)
    {
        var padding = new string('x', 1000);
        var written = new List<string>();
        long bytes = 0;
        while (bytes <= totalBytes)
        {
            var message = $"message-{written.Count:D6}-{padding}";
            writer.WriteLogEntry(LogLevel.Information, "Rolling.Test", message);
            written.Add(message);
            bytes += message.Length;
        }

        return written;
    }

    private static string[] LogFileNames(string directory) =>
        Directory.GetFiles(directory, "logs_*.jsonl").Select(Path.GetFileName).Select(name => name!).ToArray();

    private static string[] ReadLines(string directory, string fileName) =>
        File.ReadAllLines(Path.Combine(directory, fileName));

    private static string MessageOf(string line)
    {
        using var document = JsonDocument.Parse(line);
        return document.RootElement.GetProperty("message").GetString()!;
    }

    private static string CreateJsonl(string directory, string fileName, DateTime lastWriteUtc)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "{}");
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
        return path;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "telemetry-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static ActivitySamplingResult SampleAllDataAndRecorded(ref ActivityCreationOptions<ActivityContext> _) =>
        ActivitySamplingResult.AllDataAndRecorded;

    private sealed class StubTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

[TestFixture]
public class TraceEntryTests
{
    [Test]
    public void ShouldMapActivityFields_WhenConstructedFromActivity()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllData;
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("TestSource");
        using var activity = source.CreateActivity("sample", ActivityKind.Internal)?.Start();
        activity.ShouldNotBeNull();
        activity.SetTag("key", "value");

        var entry = new TraceEntry(activity, "STARTED");

        entry.Status.ShouldBe("STARTED");
        entry.Name.ShouldBe("sample");
        entry.Source.ShouldBe("TestSource");
        entry.TraceId.ShouldBe(activity.TraceId.ToString());
        entry.SpanId.ShouldBe(activity.SpanId.ToString());
        entry.Tags["key"].ShouldBe("value");
    }

    private static ActivitySamplingResult SampleAllData(ref ActivityCreationOptions<ActivityContext> _) =>
        ActivitySamplingResult.AllData;
}

[TestFixture]
public class CorrelationIdResolverTests
{
    [Test]
    public void ShouldReturnHeaderValue_WhenValidHeaderProvided()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdConstants.HeaderName] = "abc-123";

        CorrelationIdResolver.Resolve(context).ShouldBe("abc-123");
    }

    [Test]
    public void ShouldGenerateGuid_WhenHeaderMissing()
    {
        var context = new DefaultHttpContext();

        Guid.TryParse(CorrelationIdResolver.Resolve(context), out _).ShouldBeTrue();
    }

    [Test]
    public void ShouldGenerateGuid_WhenHeaderTooLong()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdConstants.HeaderName] = new string('x', 129);

        Guid.TryParse(CorrelationIdResolver.Resolve(context), out _).ShouldBeTrue();
    }
}

[TestFixture]
public class SerilogExtensionsAdditionalTests
{
    [Test]
    public void ShouldConfigureSerilog_WhenHostApplicationBuilderUsed()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddSerilogJsonConsole();

        using var host = builder.Build();
        host.Services.GetRequiredService<ILoggerFactory>().ShouldNotBeNull();
    }
}

internal sealed class StubConfiguration(string logDirectory, IReadOnlyDictionary<string, string?>? settings = null)
    : IConfiguration
{
    public string? this[string key]
    {
        get => key == "LocalTelemetry:LogDirectory" ? logDirectory : settings?.GetValueOrDefault(key);
        set => throw new NotSupportedException();
    }

    public IEnumerable<IConfigurationSection> GetChildren() => [];

    public IConfigurationSection GetSection(string key) => new StubSection(this, key);

    public IChangeToken GetReloadToken() => NullChangeToken.Singleton;

    private sealed class StubSection(StubConfiguration root, string key) : IConfigurationSection
    {
        public string Key => key.Split(':').Last();
        public string Path => key;
        public string? Value { get => root[key]; set => root[key] = value!; }

        public string? this[string subKey]
        {
            get => root[$"{Path}:{subKey}"];
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IConfigurationSection GetSection(string subKey) => new StubSection(root, $"{Path}:{subKey}");

        public IChangeToken GetReloadToken() => NullChangeToken.Singleton;
    }

    private sealed class NullChangeToken : IChangeToken
    {
        public static NullChangeToken Singleton { get; } = new();
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) =>
            EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();
        public void Dispose()
        {
        }
    }
}
