using System.Globalization;

namespace ChurchBulletin.ServiceDefaults;

internal static class TelemetryFileMaintenance
{
    /// <summary>
    /// Deletes the <c>*.jsonl</c> files of <paramref name="directory"/> last written before the retention,
    /// except the files in <paramref name="keepPaths"/> (the files a writer has open).
    /// </summary>
    public static void DeleteFilesOlderThan(string directory, int retentionDays, TimeProvider timeProvider,
        IReadOnlyCollection<string> keepPaths)
    {
        var cutoffUtc = timeProvider.GetUtcNow().UtcDateTime.AddDays(-retentionDays);

        foreach (var file in EnumerateJsonl(directory))
        {
            if (IsExpired(file, cutoffUtc, keepPaths)) TryDelete(file);
        }
    }

    private static bool IsExpired(string file, DateTime cutoffUtc, IReadOnlyCollection<string> keepPaths) =>
        !keepPaths.Contains(file) && File.GetLastWriteTimeUtc(file) < cutoffUtc;

    /// <summary>
    /// Renames <paramref name="basePath"/> to the next sequence file (<c>&lt;stem&gt;_&lt;n&gt;.jsonl</c>).
    /// A failed rename is retried on the next write.
    /// </summary>
    public static void TryMoveToNextSequence(string basePath)
    {
        try
        {
            File.Move(basePath, NextSequencePath(basePath));
        }
        catch (Exception)
        {
            // Best effort: the file stays in place and the roll is retried on the next write.
        }
    }

    internal static string NextSequencePath(string basePath)
    {
        var directory = Path.GetDirectoryName(basePath)!;
        var stem = Path.GetFileNameWithoutExtension(basePath);
        var next = Directory.GetFiles(directory, stem + "_*.jsonl")
            .Select(file => SequenceOf(file, stem))
            .DefaultIfEmpty(-1)
            .Max() + 1;
        return Path.Combine(directory, $"{stem}_{next}.jsonl");
    }

    private static int SequenceOf(string file, string stem) =>
        int.TryParse(Path.GetFileNameWithoutExtension(file).AsSpan(stem.Length + 1), NumberStyles.None,
            CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : -1;

    private static string[] EnumerateJsonl(string directory)
    {
        try
        {
            return Directory.GetFiles(directory, "*.jsonl");
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception)
        {
            // Locked or already gone: the next pass tries again.
        }
    }
}
