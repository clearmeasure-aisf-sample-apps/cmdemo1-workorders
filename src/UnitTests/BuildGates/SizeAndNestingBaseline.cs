using System.Text;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Compares a scan with the committed ratchet. The baseline is an allow-list of
/// violation keys that already exist. Keys may disappear as code is fixed; a new
/// key fails the build. Measurements are not part of the key.
/// </summary>
internal static class SizeAndNestingBaseline
{
    /// <summary>Reads baseline keys, skipping blanks and <c>#</c> comments.</summary>
    public static IReadOnlyList<string> Read(string path)
    {
        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();
    }

    /// <summary>Keys that appear more than once.</summary>
    public static IReadOnlyList<string> DuplicateKeys(IReadOnlyList<string> keys)
    {
        return keys
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>New keys and baseline keys that the scan no longer reports.</summary>
    public static SizeAndNestingDiff Diff(
        IReadOnlyList<string> current,
        IReadOnlyList<string> baseline)
    {
        var currentSet = current.ToHashSet(StringComparer.Ordinal);
        var baselineSet = baseline.ToHashSet(StringComparer.Ordinal);
        return new SizeAndNestingDiff(
            ExceptSorted(currentSet, baselineSet),
            ExceptSorted(baselineSet, currentSet));
    }

    /// <summary>Short failure text. Lists at most twenty keys on each side.</summary>
    public static string Format(SizeAndNestingDiff diff)
    {
        var builder = new StringBuilder();
        builder.Append("Size and nesting ratchet mismatch. Unexpected ");
        builder.Append(diff.Unexpected.Count);
        builder.Append(", stale ");
        builder.Append(diff.Stale.Count);
        builder.AppendLine(".");
        AppendSample(builder, "Unexpected (new violations)", diff.Unexpected);
        AppendSample(builder, "Stale (delete these baseline lines)", diff.Stale);
        return builder.ToString();
    }

    private static List<string> ExceptSorted(HashSet<string> source, HashSet<string> other)
    {
        return source
            .Where(key => !other.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();
    }

    private static void AppendSample(StringBuilder builder, string label, IReadOnlyList<string> keys)
    {
        builder.Append(label);
        builder.Append(" (");
        builder.Append(keys.Count);
        builder.AppendLine("):");
        var shown = Math.Min(keys.Count, 20);
        for (var i = 0; i < shown; i++)
        {
            builder.AppendLine(keys[i]);
        }

        if (keys.Count > shown)
        {
            builder.Append("... ");
            builder.Append(keys.Count - shown);
            builder.AppendLine(" more");
        }
    }
}

/// <summary>Result of comparing a scan to the ratchet baseline.</summary>
internal sealed record SizeAndNestingDiff(
    IReadOnlyList<string> Unexpected,
    IReadOnlyList<string> Stale)
{
    /// <summary>True when the scan and the baseline list the same keys.</summary>
    public bool IsClean => Unexpected.Count == 0 && Stale.Count == 0;
}
