namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Compares a scan with the allowlist and the ratchet baseline.
/// New ids fail. A count above the committed max fails. A lower count passes.
/// </summary>
public static class StructureRatchet
{
    /// <summary>
    /// Empty when every current finding is grandfathered or exempted and no rule exceeds its max.
    /// </summary>
    public static IReadOnlyList<string> Evaluate(
        StructureReport report,
        StructureAllowList allowList,
        StructureBaseline baseline)
    {
        var problems = new List<string>();
        foreach (var rule in StructureRule.All)
        {
            Check(problems, new RuleInput(rule, report.Findings(rule)), allowList, baseline);
        }

        return problems;
    }

    private static void Check(
        List<string> problems,
        RuleInput input,
        StructureAllowList allowList,
        StructureBaseline baseline)
    {
        var allowIds = allowList.Ids(input.Rule);
        foreach (var id in allowIds)
        {
            if (!input.Findings.Contains(id, StringComparer.Ordinal))
            {
                problems.Add($"{input.Rule}: allowlist id {id} is not a current finding");
            }
        }

        var current = input.Findings
            .Where(id => !allowIds.Contains(id, StringComparer.Ordinal))
            .ToList();
        foreach (var id in current)
        {
            if (!baseline.Ids(input.Rule).Contains(id))
            {
                problems.Add($"{input.Rule}: new id {id}");
            }
        }

        if (current.Count > baseline.Max(input.Rule))
        {
            problems.Add($"{input.Rule}: count {current.Count} exceeds max {baseline.Max(input.Rule)}");
        }
    }

    private readonly record struct RuleInput(string Rule, IReadOnlyList<string> Findings);
}
