namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Artifact uploads must time out quickly, and test-result uploads must still run after a failure.
/// </summary>
internal static class ArtifactUploadSchedule
{
    /// <summary>Problems in <paramref name="workflow"/>, the text of <c>build.yml</c>.</summary>
    public static IReadOnlyList<string> Problems(string workflow)
    {
        var problems = new List<string>();
        var sawUpload = false;
        foreach (var step in Steps(workflow))
        {
            if (!IsArtifactUpload(step))
            {
                continue;
            }

            sawUpload = true;
            RequireTimeout(problems, step);
            RequireFailurePath(problems, step);
        }

        if (!sawUpload)
        {
            problems.Add("build.yml has no artifact upload step.");
        }

        return problems;
    }

    private static void RequireTimeout(List<string> problems, string step)
    {
        if (step.Split('\n').Any(line => line.Trim() == "timeout-minutes: 3"))
        {
            return;
        }

        problems.Add($"{NameOf(step)} has no 3 minute timeout.");
    }

    private static void RequireFailurePath(List<string> problems, string step)
    {
        var name = NameOf(step);
        if (!MustRunAfterFailure(name))
        {
            return;
        }

        if (!step.Contains("if: always()", StringComparison.Ordinal))
        {
            problems.Add($"{name} must still upload when the job fails.");
        }
    }

    private static bool MustRunAfterFailure(string name)
    {
        return name.Contains("Test Results", StringComparison.Ordinal)
            || name.Contains("Code Coverage", StringComparison.Ordinal)
            || name.Contains("CRAP", StringComparison.Ordinal)
            || name.Contains("security", StringComparison.Ordinal)
            || name.Contains("SARIF", StringComparison.Ordinal);
    }

    private static bool IsArtifactUpload(string step)
    {
        return step.Contains("uses: actions/upload-artifact@", StringComparison.Ordinal)
            || step.Contains("uses: github/codeql-action/upload-sarif@", StringComparison.Ordinal);
    }

    private static string NameOf(string step)
    {
        var line = step.Split('\n').FirstOrDefault(item => item.TrimStart().StartsWith("name:", StringComparison.Ordinal));
        if (line is null)
        {
            return "(unnamed upload)";
        }

        return line.Trim()["name:".Length..].Trim();
    }

    private static IEnumerable<string> Steps(string workflow) => WorkflowYamlSteps.In(workflow);
}
