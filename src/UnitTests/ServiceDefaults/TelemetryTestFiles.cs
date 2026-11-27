namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

/// <summary>
/// Shared temp-directory, telemetry-file and fixed-clock helpers for the local telemetry file tests.
/// </summary>
internal static class TelemetryTestFiles
{
    /// <summary>The fixed UTC instant the telemetry tests run at.</summary>
    public static readonly DateTimeOffset FixedNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Creates a new, empty, uniquely named directory under the system temp path.</summary>
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "telemetry-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Writes <c>{}</c> to <paramref name="fileName"/> and sets its last write time.</summary>
    public static string CreateJsonl(string directory, string fileName, DateTime lastWriteUtc)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "{}");
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
        return path;
    }
}
