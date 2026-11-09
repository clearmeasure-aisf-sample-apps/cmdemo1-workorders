namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// A committed ratchet baseline: one key per line for each violation that already exists.
/// Blank lines and <c>#</c> comments are ignored. Keys may disappear as code is fixed; a new key
/// fails the gate, and so does a key the scan no longer reports, so the file only shrinks.
/// </summary>
public sealed class KeyRatchet
{
    private KeyRatchet(IReadOnlyList<string> keys)
    {
        Keys = keys;
    }

    /// <summary>Keys in file order, repeats included.</summary>
    public IReadOnlyList<string> Keys { get; }

    /// <summary>Parses baseline text.</summary>
    public static KeyRatchet Parse(string text)
    {
        var keys = text.TrimStart('\uFEFF')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();
        return new KeyRatchet(keys);
    }

    /// <summary>Compares the keys a scan reports with this baseline.</summary>
    public RatchetComparison Compare(IEnumerable<string> current)
    {
        var currentSet = current.ToHashSet(StringComparer.Ordinal);
        var baselineSet = Keys.ToHashSet(StringComparer.Ordinal);
        return new RatchetComparison(
            Sorted(currentSet.Where(key => !baselineSet.Contains(key))),
            Sorted(baselineSet.Where(key => !currentSet.Contains(key))),
            Sorted(Repeats()));
    }

    private IEnumerable<string> Repeats()
    {
        return Keys
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
    }

    private static List<string> Sorted(IEnumerable<string> keys)
    {
        return keys.OrderBy(key => key, StringComparer.Ordinal).ToList();
    }
}
