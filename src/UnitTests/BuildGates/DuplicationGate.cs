using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Line-clone gate for <c>src</c>. Counts normalized lines that sit in a repeated
/// block of <see cref="MinLines"/> or more. <c>Generated</c>, <c>bin</c>, and <c>obj</c> are ignored.
/// </summary>
public static class DuplicationGate
{
    public const int MinLines = 8;

    private static readonly HashSet<string> SkipDirectories = new(StringComparer.Ordinal)
    {
        "bin", "obj", "Generated",
    };

    private static readonly string[] SourceExtensions = [".cs", ".razor", ".cshtml"];

    /// <summary>
    /// Scans <paramref name="sourceRoot"/> for repeated normalized line blocks.
    /// </summary>
    public static DuplicationScan Scan(string sourceRoot)
    {
        var files = CollectFiles(sourceRoot);
        var normalized = NormalizeFiles(files);
        var duplicated = CountDuplicatedLines(IndexWindows(normalized));
        return new DuplicationScan(MinLines, TotalLines(normalized), duplicated);
    }

    /// <summary>
    /// Reads the ratchet ceiling. The committed value may only decrease.
    /// </summary>
    public static int ReadMaxDuplicatedLines(string baselinePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var minLines = document.RootElement.GetProperty("minLines").GetInt32();
        if (minLines != MinLines)
        {
            throw new InvalidOperationException(
                $"Duplication baseline minLines is {minLines}; the gate requires {MinLines}.");
        }

        return document.RootElement.GetProperty("maxDuplicatedLines").GetInt32();
    }

    /// <summary>
    /// Fails when <paramref name="duplicatedLines"/> is above the committed ceiling.
    /// </summary>
    public static void EnsureWithinCeiling(int duplicatedLines, int ceiling)
    {
        if (duplicatedLines > ceiling)
        {
            throw new InvalidOperationException(
                $"Duplicated lines {duplicatedLines} exceed the ratchet ceiling {ceiling}.");
        }
    }

    private static Dictionary<string, string[]> NormalizeFiles(List<string> files)
    {
        var normalized = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            normalized[file] = Normalize(file);
        }

        return normalized;
    }

    private static int TotalLines(Dictionary<string, string[]> normalized)
    {
        var total = 0;
        foreach (var lines in normalized.Values)
        {
            total += lines.Length;
        }

        return total;
    }

    private static List<string> CollectFiles(string sourceRoot)
    {
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(sourceRoot);
        while (pending.Count > 0)
        {
            CollectDirectory(pending, files);
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

    private static void CollectDirectory(Stack<string> pending, List<string> files)
    {
        var dir = pending.Pop();
        foreach (var sub in Directory.GetDirectories(dir))
        {
            if (!SkipDirectories.Contains(Path.GetFileName(sub)))
            {
                pending.Push(sub);
            }
        }

        foreach (var file in Directory.GetFiles(dir))
        {
            if (IsSource(file))
            {
                files.Add(file);
            }
        }
    }

    private static bool IsSource(string path)
    {
        foreach (var extension in SourceExtensions)
        {
            if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] Normalize(string path)
    {
        var lines = new List<string>();
        foreach (var raw in File.ReadLines(path))
        {
            var collapsed = Collapse(raw);
            if (collapsed.Length > 0)
            {
                lines.Add(collapsed);
            }
        }

        return lines.ToArray();
    }

    private static string Collapse(string raw)
    {
        var builder = new StringBuilder(raw.Length);
        var pendingSpace = false;
        foreach (var ch in raw.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    private static Dictionary<string, List<Location>> IndexWindows(Dictionary<string, string[]> files)
    {
        var windows = new Dictionary<string, List<Location>>(StringComparer.Ordinal);
        foreach (var (path, lines) in files)
        {
            AddWindows(windows, path, lines);
        }

        return windows;
    }

    private static void AddWindows(Dictionary<string, List<Location>> windows, string path, string[] lines)
    {
        var last = lines.Length - MinLines;
        for (var start = 0; start <= last; start++)
        {
            var hash = HashWindow(lines, start);
            if (!windows.TryGetValue(hash, out var list))
            {
                list = [];
                windows[hash] = list;
            }

            list.Add(new Location(path, start));
        }
    }

    private static string HashWindow(string[] lines, int start)
    {
        var joined = string.Join('\n', lines.Skip(start).Take(MinLines));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(joined)));
    }

    private static int CountDuplicatedLines(Dictionary<string, List<Location>> windows)
    {
        var covered = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        foreach (var locations in windows.Values)
        {
            MarkUniqueCopies(locations, covered);
        }

        var count = 0;
        foreach (var lines in covered.Values)
        {
            count += lines.Count;
        }

        return count;
    }

    private static void MarkUniqueCopies(List<Location> locations, Dictionary<string, HashSet<int>> covered)
    {
        if (locations.Count < 2)
        {
            return;
        }

        locations.Sort(CompareLocations);
        var unique = SelectNonOverlapping(locations);
        if (unique.Count < 2)
        {
            return;
        }

        foreach (var location in unique)
        {
            Mark(covered, location);
        }
    }

    private static List<Location> SelectNonOverlapping(List<Location> locations)
    {
        var unique = new List<Location>();
        foreach (var location in locations)
        {
            if (!OverlapsKept(location, unique))
            {
                unique.Add(location);
            }
        }

        return unique;
    }

    private static bool OverlapsKept(Location location, List<Location> unique)
    {
        foreach (var kept in unique)
        {
            if (SameFileOverlap(location, kept))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SameFileOverlap(Location location, Location kept)
    {
        return location.Path == kept.Path && Math.Abs(location.Start - kept.Start) < MinLines;
    }

    private static void Mark(Dictionary<string, HashSet<int>> covered, Location location)
    {
        if (!covered.TryGetValue(location.Path, out var lines))
        {
            lines = [];
            covered[location.Path] = lines;
        }

        for (var offset = 0; offset < MinLines; offset++)
        {
            lines.Add(location.Start + offset);
        }
    }

    private static int CompareLocations(Location left, Location right)
    {
        var path = string.CompareOrdinal(left.Path, right.Path);
        return path != 0 ? path : left.Start.CompareTo(right.Start);
    }

    private readonly record struct Location(string Path, int Start);
}

/// <summary>
/// Normalized-line clone totals for one source tree.
/// </summary>
public sealed record DuplicationScan(int MinLines, int TotalLines, int DuplicatedLines);
