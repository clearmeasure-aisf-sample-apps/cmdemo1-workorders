namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Splits GitHub Actions workflow (or job) YAML into step blocks that start with
/// <c>      - </c>, the same indentation <see cref="WorkflowFile"/> uses.
/// </summary>
internal static class WorkflowYamlSteps
{
    private const string StepStart = "      - ";

    /// <summary>Each step’s lines joined with newlines, in document order.</summary>
    public static IEnumerable<string> In(string yaml)
    {
        var lines = yaml.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
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
