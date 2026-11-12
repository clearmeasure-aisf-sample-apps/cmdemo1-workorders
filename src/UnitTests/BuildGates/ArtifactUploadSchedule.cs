namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Artifact uploads must time out quickly, and test-result uploads must still run after a failure.
/// </summary>
internal static class ArtifactUploadSchedule
{
    private const string StepStart = "      - ";

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

    private static IEnumerable<string> Steps(string workflow)
    {
        var lines = workflow.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var current = new List<string>();
        foreach (var line in lines)
        {
            if (current.Count > 0 && EndsStep(line))
            {
                yield return string.Join('\n', current);
                current.Clear();
            }

            if (current.Count > 0 || line.StartsWith(StepStart, StringComparison.Ordinal))
            {
                current.Add(line);
            }
        }

        if (current.Count > 0)
        {
            yield return string.Join('\n', current);
        }
    }

    private static bool EndsStep(string line)
    {
        if (line.Trim().Length == 0)
        {
            return false;
        }

        var indent = line.Length - line.TrimStart(' ').Length;
        return line.StartsWith(StepStart, StringComparison.Ordinal) || indent < StepStart.Length - 2;
    }
}
