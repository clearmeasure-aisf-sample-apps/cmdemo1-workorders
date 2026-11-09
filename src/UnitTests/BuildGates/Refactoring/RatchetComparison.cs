namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// The difference between a rule's violations and its <see cref="RatchetBaseline"/>.
/// </summary>
public sealed record RatchetComparison(IReadOnlyList<string> NewViolations, IReadOnlyList<string> StaleEntries);
