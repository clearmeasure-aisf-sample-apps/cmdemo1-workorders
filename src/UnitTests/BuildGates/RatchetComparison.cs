using System.Text;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Outcome of comparing a scan with a <see cref="KeyRatchet"/>.
/// </summary>
/// <param name="Unexpected">Keys the scan reports that the baseline does not list: new violations.</param>
/// <param name="Stale">Keys the baseline lists that the scan no longer reports: lines to delete.</param>
/// <param name="Repeated">Keys the baseline lists more than once.</param>
public sealed record RatchetComparison(
    IReadOnlyList<string> Unexpected,
    IReadOnlyList<string> Stale,
    IReadOnlyList<string> Repeated)
{
    private const int ShownPerList = 20;

    /// <summary>True when the scan and the baseline list the same keys, each once.</summary>
    public bool IsClean => Unexpected.Count == 0 && Stale.Count == 0 && Repeated.Count == 0;

    /// <summary>
    /// Failure text naming the ratchet and at most twenty keys from each list.
    /// </summary>
    public string Report(string ratchetName)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            $"{ratchetName} ratchet mismatch. Unexpected {Unexpected.Count}, stale {Stale.Count}, repeated {Repeated.Count}.");
        AppendSample(builder, "Unexpected (new violations)", Unexpected);
        AppendSample(builder, "Stale (delete these baseline lines)", Stale);
        AppendSample(builder, "Repeated (keep one of each)", Repeated);
        return builder.ToString();
    }

    private static void AppendSample(StringBuilder builder, string label, IReadOnlyList<string> keys)
    {
        builder.AppendLine($"{label} ({keys.Count}):");
        foreach (var key in keys.Take(ShownPerList))
        {
            builder.AppendLine(key);
        }

        if (keys.Count > ShownPerList)
        {
            builder.AppendLine($"... {keys.Count - ShownPerList} more");
        }
    }
}
