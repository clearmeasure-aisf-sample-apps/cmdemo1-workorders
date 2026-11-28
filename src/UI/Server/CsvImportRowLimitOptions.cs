using ClearMeasure.Bootcamp.Core.Import;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for <see cref="CsvImportRowLimitGuard"/>: the most data rows one bulk-import CSV may contain.
/// </summary>
public sealed class CsvImportRowLimitOptions
{
    /// <summary>Configuration section bound to these options.</summary>
    public const string SectionName = "CsvImportRowLimit";

    /// <summary>The most data rows (header excluded) one import may contain. Values below 1 are treated as 1.</summary>
    public int MaxRows { get; init; } = CsvImportRowLimitGuard.DefaultMaxRows;
}
