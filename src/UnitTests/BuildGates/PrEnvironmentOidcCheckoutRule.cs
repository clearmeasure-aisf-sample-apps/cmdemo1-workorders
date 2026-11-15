namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// For an OIDC job, every <c>actions/checkout</c> must pin the default branch
/// (same repo) or an allow-listed external repository — never the push branch head.
/// </summary>
internal static class PrEnvironmentOidcCheckoutRule
{
    private const string StepPrefix = "      - ";
    private const string DefaultBranchRef = "ref: ${{ github.event.repository.default_branch }}";
    private const string AllowedExternalRepo =
        "repository: clearmeasure-aisf-sample-apps/cmdemo1-system";

    /// <summary>Appends a problem for each untrusted checkout in <paramref name="job"/>.</summary>
    public static void Collect(ICollection<string> problems, WorkflowJobSlice job)
    {
        foreach (var step in CheckoutSteps(job.Text))
        {
            if (IsTrustedCheckout(step))
            {
                continue;
            }

            problems.Add(
                $"Job '{job.Id}' has id-token: write but checks out the branch head (untrusted). " +
                "Use ref: ${{ github.event.repository.default_branch }} only.");
        }
    }

    private static IEnumerable<string> CheckoutSteps(string jobText)
    {
        foreach (var step in Steps(jobText))
        {
            if (step.Contains("uses: actions/checkout@", StringComparison.Ordinal))
            {
                yield return step;
            }
        }
    }

    private static IEnumerable<string> Steps(string jobText)
    {
        var lines = jobText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var current = new List<string>();
        foreach (var line in lines)
        {
            if (current.Count > 0 && line.StartsWith(StepPrefix, StringComparison.Ordinal))
            {
                yield return string.Join('\n', current);
                current.Clear();
            }

            if (current.Count > 0 || line.StartsWith(StepPrefix, StringComparison.Ordinal))
            {
                current.Add(line);
            }
        }

        if (current.Count > 0)
        {
            yield return string.Join('\n', current);
        }
    }

    private static bool IsTrustedCheckout(string step)
    {
        if (step.Contains(AllowedExternalRepo, StringComparison.Ordinal) &&
            step.Contains("ref: main", StringComparison.Ordinal))
        {
            return true;
        }

        if (step.Contains("repository:", StringComparison.Ordinal))
        {
            return false;
        }

        return step.Contains(DefaultBranchRef, StringComparison.Ordinal);
    }
}
