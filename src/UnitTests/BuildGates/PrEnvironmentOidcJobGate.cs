using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// PR environment jobs with <c>id-token: write</c> must not check out the branch head (#1671).
/// </summary>
internal static class PrEnvironmentOidcJobGate
{
    private static readonly Regex JobHeader = new(@"^  (?<id>[A-Za-z0-9_-]+):\s*$", RegexOptions.Compiled);

    private static readonly Regex CheckoutUses = new(
        @"^\s+- uses:\s*actions/checkout@",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Problems in <paramref name="workflow"/> when an OIDC job checks out the push SHA.</summary>
    public static IReadOnlyList<string> Problems(string workflow)
    {
        var problems = new List<string>();
        var normalized = workflow.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = normalized.Split('\n');
        var jobsIndex = Array.FindIndex(lines, line => line == "jobs:");
        if (jobsIndex < 0)
        {
            problems.Add("workflow has no jobs: key");
            return problems;
        }

        var jobStarts = new List<int>();
        for (var i = jobsIndex + 1; i < lines.Length; i++)
        {
            if (JobHeader.IsMatch(lines[i]))
            {
                jobStarts.Add(i);
            }
        }

        for (var j = 0; j < jobStarts.Count; j++)
        {
            var start = jobStarts[j];
            var end = j + 1 < jobStarts.Count ? jobStarts[j + 1] : lines.Length;
            var jobId = JobHeader.Match(lines[start]).Groups["id"].Value;
            var jobText = string.Join('\n', lines.AsSpan(start, end - start).ToArray());
            if (!jobText.Contains("id-token: write", StringComparison.Ordinal))
            {
                continue;
            }

            AddCheckoutProblems(problems, jobId, jobText);
        }

        return problems;
    }

    private static void AddCheckoutProblems(List<string> problems, string jobId, string jobText)
    {
        var matches = CheckoutUses.Matches(jobText);
        if (matches.Count == 0)
        {
            return;
        }

        foreach (Match match in matches)
        {
            var block = CheckoutBlock(jobText, match.Index);
            if (IsTrustedCheckout(block))
            {
                continue;
            }

            problems.Add(
                $"Job '{jobId}' has id-token: write but checks out the branch head (untrusted). " +
                "Use ref: ${{ github.event.repository.default_branch }} only.");
        }
    }

    private static string CheckoutBlock(string jobText, int usesIndex)
    {
        var fromUses = jobText[usesIndex..];
        var nextStep = Regex.Match(fromUses, @"\n      - ", RegexOptions.None);
        if (!nextStep.Success)
        {
            return fromUses;
        }

        return fromUses[..nextStep.Index];
    }

    private static bool IsTrustedCheckout(string block)
    {
        if (block.Contains("repository:", StringComparison.Ordinal))
        {
            return true;
        }

        return block.Contains(
            "ref: ${{ github.event.repository.default_branch }}",
            StringComparison.Ordinal);
    }
}
