using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using HomeIndex = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class AccessibilityAxeTests : AcceptanceTestBase
{
    private static readonly string[] Themes = ["light", "dark"];

    [Test]
    [Retry(2)]
    public async Task Should_HaveNoSeriousOrCriticalAxeViolations_OnEveryRouteInBothThemes()
    {
        var failures = new List<string>();

        foreach (var theme in Themes)
        {
            await ScanAsync(theme, "/login", WaitForTestId(nameof(Login.Elements.User)), failures);
        }

        await Page.GotoAsync("/");
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();
        order.Number.ShouldNotBeNullOrWhiteSpace();
        var number = Uri.EscapeDataString(order.Number);

        var routes = new (string Url, Func<Task> Ready)[]
        {
            ("/", WaitForTestId(nameof(HomeIndex.Elements.StatusDashboard))),
            ("/not-found", WaitForTestId(nameof(NotFound.Elements.NotFoundHomeLink))),
            ("/_clienthealthcheck", WaitForTestId(nameof(ClientHealthCheck.Elements.Status))),
            ("/_clienthealthcheck/detailed", WaitForTestId(nameof(DetailedClientHealthCheck.Elements.Status))),
            ("/settings", WaitForTestId(nameof(Settings.Elements.DarkModeSwitch))),
            ("/workorder/search", WaitForTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill))),
            ("/workorder/manage?mode=New", WaitForTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber))),
            ($"/workorder/manage/{number}?mode=Edit", WaitForTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber))),
            ($"/workorder/print/{number}", WaitForTestId(nameof(WorkOrderPrint.Elements.Number))),
            ("/admin/buildings", WaitUntil(".buildings-header", ".buildings-loading")),
            ("/admin/teams", WaitUntil(".teams-header", ".teams-loading")),
            ("/reports", WaitForTestId(nameof(Reports.Elements.SummaryCardStatus))),
            ("/ai-agent", WaitForTestId(nameof(ApplicationChat.Elements.ChatShell)))
        };

        foreach (var theme in Themes)
        {
            foreach (var route in routes)
            {
                await ScanAsync(theme, route.Url, route.Ready, failures);
            }
        }

        failures.ShouldBeEmpty();
    }

    private Func<Task> WaitForTestId(string testId)
    {
        return () => Page.GetByTestId(testId).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
    }

    private Func<Task> WaitUntil(string presentSelector, string absentSelector)
    {
        return () => Page.WaitForFunctionAsync(
            "(selectors) => { const present = document.querySelector(selectors.present); const absent = document.querySelector(selectors.absent); return !!present && !absent; }",
            new { present = presentSelector, absent = absentSelector });
    }

    private async Task ScanAsync(string theme, string url, Func<Task> ready, List<string> failures)
    {
        await Page.AddInitScriptAsync(
            $"localStorage.setItem('churchbulletin-theme', '{theme}');");
        await Page.GotoAsync(url);
        await ready();

        var actual = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-theme')");
        actual.ShouldBe(theme, $"theme for {url}");

        var result = await Page.RunAxe();
        CollectSeriousOrCritical(result, theme, url, failures);
    }

    private static void CollectSeriousOrCritical(AxeResult result, string theme, string url, List<string> failures)
    {
        if (result.Violations == null)
        {
            return;
        }

        foreach (var violation in result.Violations)
        {
            var impact = violation.Impact ?? string.Empty;
            if (!impact.Equals("serious", StringComparison.OrdinalIgnoreCase)
                && !impact.Equals("critical", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nodes = violation.Nodes;
            if (nodes == null || !nodes.Any())
            {
                failures.Add($"{theme} {url} {violation.Id} {impact}");
                continue;
            }

            foreach (var node in nodes)
            {
                var target = node.Target == null ? string.Empty : string.Join(" ", node.Target);
                failures.Add($"{theme} {url} {violation.Id} {impact} {target}");
            }
        }
    }
}
