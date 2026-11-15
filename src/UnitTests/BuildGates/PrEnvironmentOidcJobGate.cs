namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// PR environment jobs with <c>id-token: write</c> must not check out the branch head (#1671).
/// </summary>
internal static class PrEnvironmentOidcJobGate
{
    /// <summary>Problems in <paramref name="workflow"/> when an OIDC job checks out the push SHA.</summary>
    public static IReadOnlyList<string> Problems(string workflow)
    {
        var problems = new List<string>();
        var jobs = PrEnvironmentWorkflowJobs.All(workflow);
        if (jobs.Count == 0)
        {
            problems.Add("workflow has no jobs: key");
            return problems;
        }

        CollectOidcCheckoutProblems(problems, jobs);
        return problems;
    }

    private static void CollectOidcCheckoutProblems(
        List<string> problems,
        IReadOnlyList<WorkflowJobSlice> jobs)
    {
        foreach (var job in jobs)
        {
            if (!job.HasIdTokenWrite)
            {
                continue;
            }

            PrEnvironmentOidcCheckoutRule.Collect(problems, job);
        }
    }
}
