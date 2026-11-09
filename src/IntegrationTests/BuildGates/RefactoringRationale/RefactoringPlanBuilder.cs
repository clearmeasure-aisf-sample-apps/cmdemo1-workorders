namespace ClearMeasure.Bootcamp.IntegrationTests.BuildGates.RefactoringRationale;

/// <summary>
/// The "## Refactoring plan" section of a pull request body. By default it is a valid plan for extracting
/// <c>WorkOrderDraft</c> out of <c>WorkOrderTools</c>: one rationale row, a Before/After class-diagram pair, and the
/// line-count table the script generates.
/// </summary>
public sealed class RefactoringPlanBuilder
{
    public const string ValidRow =
        "| Long Parameter List, Data Clumps | Introduce Parameter Object | WorkOrderDraft | The ten create arguments always travel together and are validated together |";

    private const string BeforeDiagram = "classDiagram\n    class WorkOrderTools";
    private const string AfterDiagram = "classDiagram\n    WorkOrderTools --> WorkOrderDraft";

    private readonly List<string> _rationaleRows = [ValidRow];
    private readonly List<(string Label, string Mermaid)> _diagrams = [("Before", BeforeDiagram), ("After", AfterDiagram)];
    private string? _lineCounts;
    private bool _generateLineCounts = true;

    public RefactoringPlanBuilder WithRationaleRow(string row)
    {
        _rationaleRows.Clear();
        _rationaleRows.Add(row);
        return this;
    }

    public RefactoringPlanBuilder WithDiagrams(params (string Label, string Mermaid)[] diagrams)
    {
        _diagrams.Clear();
        _diagrams.AddRange(diagrams);
        return this;
    }

    public RefactoringPlanBuilder WithLineCounts(string table)
    {
        _lineCounts = table;
        _generateLineCounts = false;
        return this;
    }

    public RefactoringPlanBuilder WithoutLineCounts()
    {
        _lineCounts = null;
        _generateLineCounts = false;
        return this;
    }

    /// <summary>True when the body should carry the table the script generates for the pull request.</summary>
    public bool UsesGeneratedLineCounts => _generateLineCounts;

    public string Build(string generatedLineCounts)
    {
        var lineCounts = _generateLineCounts ? generatedLineCounts : _lineCounts;
        var diagrams = _diagrams.Select(diagram => $"{diagram.Label}\n\n```mermaid\n{diagram.Mermaid}\n```\n");
        var lineCountSection = lineCounts is null ? "" : $"### Line counts\n\n{lineCounts}\n";
        return $"""
                ## Summary

                Refactors the work-order tools.

                ## Refactoring plan

                <!-- One row per move. -->
                ### Refactoring rationale

                | Smell | Refactoring | New abstraction | Why |
                |-------|-------------|-----------------|-----|
                {string.Join('\n', _rationaleRows)}

                ### Before and after

                {string.Join('\n', diagrams)}
                {lineCountSection}
                ## Testing

                Unit tests.
                """;
    }
}
