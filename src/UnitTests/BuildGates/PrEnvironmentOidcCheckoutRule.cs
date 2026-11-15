using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// For an OIDC job, every <c>actions/checkout</c> must use the default branch
/// (or another repository), never the push branch head.
/// </summary>
internal static class PrEnvironmentOidcCheckoutRule
{
    private static readonly Regex CheckoutUses = new(
        @"^\s+- uses:\s*actions/checkout@",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Appends a problem for each untrusted checkout in <paramref name="job"/>.</summary>
    public static void Collect(ICollection<string> problems, WorkflowJobSlice job)
    {
        foreach (Match match in CheckoutUses.Matches(job.Text))
        {
            var block = CheckoutBlock(job.Text, match.Index);
            if (IsTrustedCheckout(block))
            {
                continue;
            }

            problems.Add(
                $"Job '{job.Id}' has id-token: write but checks out the branch head (untrusted). " +
                "Use ref: ${{ github.event.repository.default_branch }} only.");
        }
    }

    private static string CheckoutBlock(string jobText, int usesIndex)
    {
        var fromUses = jobText[usesIndex..];
        var nextStep = Regex.Match(fromUses, @"\n      - ", RegexOptions.None);
        return nextStep.Success ? fromUses[..nextStep.Index] : fromUses;
    }

    private static bool IsTrustedCheckout(string block) =>
        block.Contains("repository:", StringComparison.Ordinal) ||
        block.Contains("ref: ${{ github.event.repository.default_branch }}", StringComparison.Ordinal);
}
