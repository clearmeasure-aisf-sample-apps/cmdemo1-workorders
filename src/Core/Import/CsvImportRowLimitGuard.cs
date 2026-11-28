namespace ClearMeasure.Bootcamp.Core.Import;

/// <summary>
/// Caps how many data rows one bulk-import CSV may contain, so an anonymous upload cannot create an unbounded
/// number of work orders. The parser checks it while reading and stops at the first row past the limit.
/// </summary>
public sealed class CsvImportRowLimitGuard
{
    /// <summary>Data rows allowed in one import when configuration does not set a limit.</summary>
    public const int DefaultMaxRows = 500;

    /// <summary>Creates a guard allowing at most <paramref name="maxRows"/> data rows; values below 1 are treated as 1.</summary>
    public CsvImportRowLimitGuard(int maxRows)
    {
        MaxRows = Math.Max(1, maxRows);
    }

    /// <summary>A guard using <see cref="DefaultMaxRows"/>.</summary>
    public static CsvImportRowLimitGuard Default { get; } = new(DefaultMaxRows);

    /// <summary>The most data rows (header excluded) one import may contain.</summary>
    public int MaxRows { get; }

    /// <summary>The error returned when an import has more than <see cref="MaxRows"/> data rows.</summary>
    public string OverLimitMessage =>
        $"CSV contains more than the maximum of {MaxRows} data rows allowed in one import.";

    /// <summary>True when an import with <paramref name="rowCount"/> data rows is within the limit.</summary>
    public bool Allows(int rowCount) => rowCount <= MaxRows;
}
