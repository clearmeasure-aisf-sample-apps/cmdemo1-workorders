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
        RejectStaleAllowances(problems, input, allowList);
        var current = WithoutAllowances(input, allowList);
        RejectNewFindings(problems, input.Rule, current, baseline);
        if (current.Count > baseline.Max(input.Rule))
        {
            problems.Add($"{input.Rule}: count {current.Count} exceeds max {baseline.Max(input.Rule)}");
        }
    }

    private static void RejectStaleAllowances(
        List<string> problems,
        RuleInput input,
        StructureAllowList allowList)
    {
        foreach (var id in allowList.Ids(input.Rule))
        {
            if (!input.Findings.Contains(id, StringComparer.Ordinal))
            {
                problems.Add($"{input.Rule}: allowlist id {id} is not a current finding");
            }
        }
    }

    private static List<string> WithoutAllowances(RuleInput input, StructureAllowList allowList)
    {
        var allowIds = allowList.Ids(input.Rule);
        return input.Findings.Where(id => !allowIds.Contains(id, StringComparer.Ordinal)).ToList();
    }

    private static void RejectNewFindings(
        List<string> problems,
        string rule,
        List<string> current,
        StructureBaseline baseline)
    {
        foreach (var id in current)
        {
            if (!baseline.Ids(rule).Contains(id))
            {
                problems.Add($"{rule}: new id {id}");
            }
        }
    }

    private readonly record struct RuleInput(string Rule, IReadOnlyList<string> Findings);
}
