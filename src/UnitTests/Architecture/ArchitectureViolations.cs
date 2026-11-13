using ArchUnitNET.Fluent;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Evaluates an ArchUnitNET rule against <see cref="SolutionArchitecture.Current"/> and lists every violation,
/// so a failing test names each offending type instead of only the rule.
/// </summary>
public static class ArchitectureViolations
{
    /// <summary>One line per violating type; empty when the rule holds.</summary>
    public static IReadOnlyList<string> Of(IArchRule rule)
    {
        return rule.Evaluate(SolutionArchitecture.Current)
            .Where(result => !result.Passed)
            .Select(result => result.Description)
            .ToList();
    }
}
