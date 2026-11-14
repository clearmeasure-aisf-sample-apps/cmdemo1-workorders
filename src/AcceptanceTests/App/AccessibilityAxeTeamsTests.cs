using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Teams-page axe scan in the serial lane so peer fixtures that create member avatars
/// finish (and TearDown) before this route runs. Does not delete other tests' rows.
/// </summary>
[TestFixture, NonParallelizable, Category("SerialLane")]
public class AccessibilityAxeTeamsTests : AppAcceptanceTestBase
{
    /// <summary>Scans while signed in; storage-state sign-in is not used.</summary>
    protected override bool UseStorageStateSignIn => false;

    private static readonly string[] Themes = ["light", "dark"];

    [Test]
    [Retry(2)]
    public async Task Should_HaveNoSeriousOrCriticalAxeViolations_OnTeamsRoute()
    {
        var route = AccessibilityAxeRoutes.ByKey("teams");
        var failures = new List<string>();
        await Page.GotoAsync("/");
        await LoginAsCurrentUser();
        await ScanLightAndDarkAsync(route, failures);
        failures.ShouldBeEmpty();
    }

    private async Task ScanLightAndDarkAsync(AccessibilityAxeRoute route, List<string> failures)
    {
        foreach (var theme in Themes)
        {
            await ScanAsync(theme, route.UrlTemplate, ReadyFor(route), failures);
        }
    }

    private Func<Task> ReadyFor(AccessibilityAxeRoute route) =>
        () => Page.WaitForFunctionAsync(
            "(selectors) => { const present = document.querySelector(selectors.present); const absent = document.querySelector(selectors.absent); return !!present && !absent; }",
            new
            {
                present = route.ReadyToken.Split('|')[0],
                absent = route.ReadyToken.Split('|')[1]
            });

    private async Task ScanAsync(string theme, string url, Func<Task> ready, List<string> failures)
    {
        await Page.AddInitScriptAsync($"localStorage.setItem('churchbulletin-theme', '{theme}');");
        await Page.GotoAsync(url);
        await ready();
        var actual = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-theme')");
        actual.ShouldBe(theme, $"theme for {url}");
        CollectSeriousOrCritical(await Page.RunAxe(), theme, url, failures);
    }

    private static void CollectSeriousOrCritical(AxeResult result, string theme, string url, List<string> failures)
    {
        if (result.Violations == null)
        {
            return;
        }

        foreach (var violation in result.Violations)
        {
            AppendIfSeriousOrCritical(violation, theme, url, failures);
        }
    }

    private static void AppendIfSeriousOrCritical(
        AxeResultItem violation,
        string theme,
        string url,
        List<string> failures)
    {
        var impact = violation.Impact ?? string.Empty;
        if (!impact.Equals("serious", StringComparison.OrdinalIgnoreCase)
            && !impact.Equals("critical", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var nodes = violation.Nodes;
        if (nodes == null || !nodes.Any())
        {
            failures.Add($"{theme} {url} {violation.Id} {impact}");
            return;
        }

        foreach (var node in nodes)
        {
            var target = node.Target == null ? string.Empty : string.Join(" ", node.Target);
            failures.Add($"{theme} {url} {violation.Id} {impact} {target}");
        }
    }
}
