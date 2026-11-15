using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>Splits <c>pr-environment.yml</c>-style workflow text into job slices.</summary>
internal static class PrEnvironmentWorkflowJobs
{
    private static readonly Regex JobHeader = new(@"^  (?<id>[A-Za-z0-9_-]+):\s*$", RegexOptions.Compiled);

    /// <summary>Every job under <c>jobs:</c>, or an empty list when the key is missing.</summary>
    public static IReadOnlyList<WorkflowJobSlice> All(string workflow)
    {
        var normalized = workflow.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = normalized.Split('\n');
        var jobsIndex = Array.FindIndex(lines, line => line == "jobs:");
        if (jobsIndex < 0)
        {
            return Array.Empty<WorkflowJobSlice>();
        }

        return SlicesFrom(lines, jobsIndex);
    }

    /// <summary>Jobs that declare <c>id-token: write</c>.</summary>
    public static IReadOnlyList<WorkflowJobSlice> WithIdTokenWrite(string workflow) =>
        All(workflow).Where(job => job.HasIdTokenWrite).ToList();

    private static IReadOnlyList<WorkflowJobSlice> SlicesFrom(string[] lines, int jobsIndex)
    {
        var starts = JobStartIndexes(lines, jobsIndex);
        var slices = new List<WorkflowJobSlice>(starts.Count);
        for (var i = 0; i < starts.Count; i++)
        {
            var start = starts[i];
            var end = i + 1 < starts.Count ? starts[i + 1] : lines.Length;
            var id = JobHeader.Match(lines[start]).Groups["id"].Value;
            var text = string.Join('\n', lines[start..end]);
            slices.Add(new WorkflowJobSlice(id, text));
        }

        return slices;
    }

    private static List<int> JobStartIndexes(string[] lines, int jobsIndex)
    {
        var starts = new List<int>();
        for (var i = jobsIndex + 1; i < lines.Length; i++)
        {
            if (JobHeader.IsMatch(lines[i]))
            {
                starts.Add(i);
            }
        }

        return starts;
    }
}
