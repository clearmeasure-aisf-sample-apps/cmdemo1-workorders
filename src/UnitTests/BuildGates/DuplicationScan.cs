namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Normalized-line clone totals for one source tree.
/// </summary>
public sealed record DuplicationScan(int MinLines, int TotalLines, int DuplicatedLines);
