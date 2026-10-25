using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>At 390px the document matches the viewport on every route, and wide regions scroll inside themselves.</summary>
[TestFixture]
public class PhoneWidthNoDocumentScrollTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldMatchDocumentWidthToViewport_At390_OnEveryRoute()
    {
        await Page.SetViewportSizeAsync(390, 844);

        await Page.GotoAsync("/login");
        await Expect(Page.GetByTestId(nameof(Login.Elements.LoginHint))).ToBeVisibleAsync();
        await AssertDocumentFitsAsync("/login");

        await LoginAsCurrentUser();
        var number = await SeedWorkOrderAsync();

        await OpenAndAssertFitsAsync("/", Page.GetByTestId(nameof(Index.Elements.HomeTagline)));
        await OpenAndAssertFitsAsync("/workorder/search", Page.Locator(".table-scroll"));
        await AssertScrollsInsideAsync(".table-scroll");
        await AssertTopBarWrapsAsync();

        await OpenAndAssertFitsAsync("/workorder/search?View=Board", Page.Locator(".board-scroll"));
        await AssertScrollsInsideAsync(".board-scroll");

        await OpenAndAssertFitsAsync(
            "/workorder/manage?mode=New",
            Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)));
        await OpenAndAssertFitsAsync(
            $"/workorder/print/{number}",
            Page.GetByTestId(nameof(WorkOrderPrint.Elements.Number)));
        await OpenAndAssertFitsAsync("/reports", Page.GetByTestId(nameof(Reports.Elements.ChartCompletedPerWeek)));
        await AssertScrollsInsideAsync(".chart-container");
        await OpenAndAssertFitsAsync("/admin/buildings", Page.GetByRole(AriaRole.Heading, new() { Name = "Buildings & Rooms" }));
        await Expect(Page.Locator(".buildings-loading")).ToHaveCountAsync(0);
        await AssertDocumentFitsAsync("/admin/buildings");
        await OpenAndAssertFitsAsync("/admin/teams", Page.GetByRole(AriaRole.Heading, new() { Name = "Teams & Vendors" }));
        await Expect(Page.Locator(".teams-loading")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".teams-table")).ToBeVisibleAsync();
        await AssertDocumentFitsAsync("/admin/teams");
        await AssertScrollsInsideAsync(".teams-page .table-scroll");
        await OpenAndAssertFitsAsync("/ai-agent", Page.GetByTestId(nameof(ApplicationChat.Elements.ChatShell)));
        await OpenAndAssertFitsAsync("/settings", Page.GetByTestId(nameof(Settings.Elements.SettingsSubtitle)));
        await OpenAndAssertFitsAsync("/counter", Page.GetByTestId(nameof(Counter.Elements.CounterHint)));
        await OpenAndAssertFitsAsync("/fetchdata", Page.GetByTestId(nameof(FetchData.Elements.ForecastTableTooltip)));
        await OpenAndAssertFitsAsync("/not-found", Page.GetByTestId(nameof(NotFound.Elements.NotFoundHomeLink)));
        await OpenAndAssertFitsAsync("/_clienthealthcheck", Page.GetByTestId(nameof(ClientHealthCheck.Elements.Status)));
        await OpenAndAssertFitsAsync(
            "/_clienthealthcheck/detailed",
            Page.GetByTestId(nameof(DetailedClientHealthCheck.Elements.Status)));

        await Page.GotoAsync("/workorder/search");
        await Expect(Page.Locator(".table-scroll")).ToBeVisibleAsync();
        await Page.GetByTestId(nameof(MainLayout.Elements.DarkModeToggle)).ClickAsync();
        await Page.WaitForFunctionAsync("() => document.documentElement.getAttribute('data-theme') === 'dark'");

        await OpenAndAssertFitsAsync("/workorder/search", Page.Locator(".table-scroll"));
        await OpenAndAssertFitsAsync("/workorder/search?View=Board", Page.Locator(".board-scroll"));
        await OpenAndAssertFitsAsync("/reports", Page.GetByTestId(nameof(Reports.Elements.ChartCompletedPerWeek)));
    }

    private async Task OpenAndAssertFitsAsync(string url, ILocator ready)
    {
        await Page.GotoAsync(url);
        await Expect(ready).ToBeVisibleAsync();
        await AssertDocumentFitsAsync(url);
    }

    private async Task AssertDocumentFitsAsync(string url)
    {
        var measured = await Page.EvaluateAsync<string>(
            "() => document.documentElement.scrollWidth + ':' + window.innerWidth");
        var parts = measured.Split(':');
        parts[0].ShouldBe(parts[1], url);
    }

    private async Task AssertScrollsInsideAsync(string selector)
    {
        var scrollsInside = await Page.Locator(selector).EvaluateAsync<bool>(
            "element => element.scrollWidth > element.clientWidth");
        scrollsInside.ShouldBeTrue();
    }

    private async Task AssertTopBarWrapsAsync()
    {
        var wraps = await Page.Locator(".header-content").EvaluateAsync<bool>(
            "element => getComputedStyle(element).flexWrap === 'wrap'");
        wraps.ShouldBeTrue();

        var inside = await Page.EvaluateAsync<bool>(
            """
            () => {
                const limit = window.innerWidth + 1;
                return [...document.querySelectorAll('.header-content > *')]
                    .every(child => child.getBoundingClientRect().right <= limit);
            }
            """);
        inside.ShouldBeTrue();
    }

    private async Task<string> SeedWorkOrderAsync()
    {
        var number = TestData.Number("P");
        await using var context = TestHost.NewDbContext();
        context.Attach(CurrentUser);
        context.Add(new WorkOrder
        {
            Number = number,
            Creator = CurrentUser,
            Assignee = CurrentUser,
            Status = WorkOrderStatus.Draft,
            Title = $"[{TestTag}] Replace hallway light fixtures near the fellowship hall entrance",
            Building = "Sanctuary",
            RoomNumber = "Fellowship Hall 2",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            CreatedDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return number;
    }
}
