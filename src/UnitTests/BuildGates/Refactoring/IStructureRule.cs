namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// A banned move from <c>docs/refactoring-oversized-code.md</c>, checked over the source tree.
/// </summary>
public interface IStructureRule
{
    /// <summary>File name of the rule's ratchet baseline under <c>Baselines/</c>.</summary>
    string BaselineFileName { get; }

    /// <summary>What a violation means and what to do instead, shown when the gate fails.</summary>
    string Guidance { get; }

    IEnumerable<StructureViolation> FindViolations(SourceSet sources);
}
