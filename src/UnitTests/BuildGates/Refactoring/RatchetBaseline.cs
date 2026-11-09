namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// A committed list of today's violations of one rule. It may only shrink: a violation missing from the list fails,
/// and so does a line that is no longer a violation (delete it). Blank lines and <c>#</c> comments are ignored.
/// </summary>
public sealed class RatchetBaseline
{
    private readonly HashSet<string> _entries;

    private RatchetBaseline(IEnumerable<string> lines)
    {
        _entries = lines
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToHashSet(StringComparer.Ordinal);
    }

    public static RatchetBaseline Load(string path) =>
        new(File.Exists(path) ? File.ReadAllLines(path) : []);

    public static RatchetBaseline Of(params string[] entries) => new(entries);

    public RatchetComparison Compare(IEnumerable<StructureViolation> violations)
    {
        var found = violations.Select(violation => violation.Key).ToHashSet(StringComparer.Ordinal);
        return new RatchetComparison(
            found.Except(_entries).Order(StringComparer.Ordinal).ToArray(),
            _entries.Except(found).Order(StringComparer.Ordinal).ToArray());
    }
}
