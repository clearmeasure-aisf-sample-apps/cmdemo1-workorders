using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// A GitHub Actions workflow under <c>.github/workflows</c>, read as text and cut into its jobs and steps
/// by indentation: jobs at two spaces below <c>jobs:</c>, steps as the <c>- </c> items at six spaces.
/// </summary>
public sealed class WorkflowFile
{
    private const string StepStart = "      - ";

    private static readonly Regex JobHeader = new(@"^  [A-Za-z0-9_-]+:\s*$", RegexOptions.Compiled);

    private static readonly Regex StepName = new(
        @"^      (- |  )name: (?<name>.+)$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private WorkflowFile(string text)
    {
        Text = text;
    }

    /// <summary>The whole workflow, or one job of it.</summary>
    public string Text { get; }

    /// <summary>Reads <c>.github/workflows/<paramref name="fileName"/></c> from <paramref name="checkout"/>.</summary>
    public static WorkflowFile Read(RepositoryCheckout checkout, string fileName)
    {
        return new WorkflowFile(checkout.ReadText($".github/workflows/{fileName}").Replace("\r\n", "\n"));
    }

    /// <summary>The block of job <paramref name="jobId"/>, up to the next job.</summary>
    public WorkflowFile Job(string jobId)
    {
        var lines = Lines();
        var jobs = lines.IndexOf("jobs:");
        var start = jobs < 0 ? -1 : lines.FindIndex(jobs + 1, line => line.TrimEnd() == $"  {jobId}:");
        if (start < 0)
        {
            throw new InvalidOperationException($"The workflow has no job '{jobId}'.");
        }

        var end = lines.FindIndex(start + 1, JobHeader.IsMatch);
        return new WorkflowFile(Join(lines, start, end < 0 ? lines.Count : end));
    }

    /// <summary>Step names in the order the steps run.</summary>
    public IReadOnlyList<string> StepNames()
    {
        return Steps().Select(NameOf).Where(name => name.Length > 0).ToList();
    }

    /// <summary>The block of the step named <paramref name="stepName"/>.</summary>
    public string Step(string stepName)
    {
        return Steps().FirstOrDefault(step => NameOf(step) == stepName)
            ?? throw new InvalidOperationException($"The workflow has no step '{stepName}'.");
    }

    private List<string> Steps()
    {
        var lines = Lines();
        var boundaries = Enumerable.Range(0, lines.Count).Where(index => EndsStep(lines[index])).Append(lines.Count).ToList();
        return boundaries
            .Zip(boundaries.Skip(1), (start, end) => (Start: start, End: end))
            .Where(block => block.Start < lines.Count && lines[block.Start].StartsWith(StepStart, StringComparison.Ordinal))
            .Select(block => Join(lines, block.Start, block.End))
            .ToList();
    }

    private static bool EndsStep(string line)
    {
        var indent = line.Length - line.TrimStart(' ').Length;
        return line.StartsWith(StepStart, StringComparison.Ordinal) || (line.Trim().Length > 0 && indent < StepStart.Length - 2);
    }

    private static string NameOf(string step)
    {
        var match = StepName.Match(step);
        return match.Success ? match.Groups["name"].Value.Trim() : "";
    }

    private List<string> Lines()
    {
        return Text.Split('\n').ToList();
    }

    private static string Join(List<string> lines, int start, int end)
    {
        return string.Join('\n', lines.GetRange(start, end - start));
    }
}
