using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using System.Collections;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Per-route axe scans so cases can run under <see cref="ParallelScope.All"/> across workers.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
public class AccessibilityAxeTests : AppAcceptanceTestBase
{
    /// <summary>Scans /login while anonymous; authenticated routes sign in in the case.</summary>
    protected override bool UseStorageStateSignIn => false;

    private static readonly string[] Themes = ["light", "dark"];

    /// <summary>One case per axe route key.</summary>
    public static IEnumerable RouteKeys()
    {
        yield return "login";
        yield return "home";
        yield return "not-found";
        yield return "client-health";
        yield return "client-health-detailed";
        yield return "settings";
        yield return "search";
        yield return "manage-new";
        yield return "manage-edit";
        yield return "print";
        yield return "buildings";
        yield return "teams";
        yield return "reports";
        yield return "ai-agent";
    }

    [TestCaseSource(nameof(RouteKeys))]
    [Retry(2)]
    public async Task Should_HaveNoSeriousOrCriticalAxeViolations_OnRoute(string routeKey)
    {
        var route = AccessibilityAxeRoutes.ByKey(routeKey);
        var failures = new List<string>();
        var url = await ResolveUrlAsync(route);
        await SignInIfNeededAsync(route);
        await ScanLightAndDarkAsync(route, url, failures);
        failures.ShouldBeEmpty();
    }

    private async Task SignInIfNeededAsync(AccessibilityAxeRoute route)
    {
        if (!route.RequiresSignIn)
        {
            return;
        }

        await Page.GotoAsync("/");
        await LoginAsCurrentUser();
    }

    private async Task ScanLightAndDarkAsync(AccessibilityAxeRoute route, string url, List<string> failures)
    {
        foreach (var theme in Themes)
        {
            await ScanAsync(theme, url, ReadyFor(route), failures);
        }
    }

    private async Task<string> ResolveUrlAsync(AccessibilityAxeRoute route)
    {
        if (route.Key == "search")
        {
            // Own Medium row only — peer High-priority badges fail color-contrast under axe.
            await Mother.WorkOrderAsync(CurrentUser.UserName, WorkOrderStatus.Draft, new WorkOrderSeedFields
            {
                Title = $"[{TestTag}] axe search",
                Priority = WorkOrderPriority.Medium
            });
            return $"/workorder/search?Q={TestTag}";
        }

        if (route.Key == "teams")
        {
            // Peer leftover team members add avatars; initials vs aria-label fail axe.
            AcceptanceTeams.ClearAll();
            return route.UrlTemplate;
        }

        if (!route.NeedsWorkOrderNumber)
        {
            return route.UrlTemplate;
        }

        var workOrder = await Mother.DraftWorkOrderAsync(CurrentUser.UserName);
        workOrder.Number.ShouldNotBeNullOrWhiteSpace();
        return route.UrlTemplate.Replace("{number}", Uri.EscapeDataString(workOrder.Number), StringComparison.Ordinal);
    }

    private Func<Task> ReadyFor(AccessibilityAxeRoute route) =>
        route.WaitKind switch
        {
            AccessibilityAxeRoute.ReadyKind.TestId => WaitForTestId(route.ReadyToken),
            AccessibilityAxeRoute.ReadyKind.PresentAbsent => WaitUntil(
                route.ReadyToken.Split('|')[0],
                route.ReadyToken.Split('|')[1]),
            _ => throw new ArgumentOutOfRangeException(nameof(route), route.WaitKind, "Unknown ready kind.")
        };

    private Func<Task> WaitForTestId(string testId) =>
        () => Page.GetByTestId(testId).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });

    private Func<Task> WaitUntil(string presentSelector, string absentSelector) =>
        () => Page.WaitForFunctionAsync(
            "(selectors) => { const present = document.querySelector(selectors.present); const absent = document.querySelector(selectors.absent); return !!present && !absent; }",
            new { present = presentSelector, absent = absentSelector });

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
