using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class DashboardTests : AppAcceptanceTestBase
{
    /// <summary>Includes signed-out layout coverage; signed-in tests call LoginAsCurrentUser.</summary>
    protected override bool UseStorageStateSignIn => false;

    [Test, Retry(2)]
    public async Task ShouldShowHeaderAndCards_WithoutHero()
    {
        await OpenDashboardAsync();

        await Expect(Page.Locator(".page-heading h1")).ToHaveTextAsync(".NET Conf Work Orders");
        foreach (var card in Enum.GetNames<WorkOrderSummaryCards.Elements>())
        {
            await Expect(Page.GetByTestId(card)).ToBeVisibleAsync();
        }

        foreach (var status in WorkOrderStatus.GetAllItems())
        {
            await Expect(Page.GetByTestId(nameof(IndexPage.Elements.StatusCard) + status.Key)).ToBeVisibleAsync();
        }

        await Expect(Page.Locator(".church-hero, .church-illustration, .lovejoy-quote, .info-card")).ToHaveCountAsync(0);
        (await Page.Locator("a.summary-card").First.EvaluateAsync<string>("e => getComputedStyle(e).textDecorationLine"))
            .ShouldBe("none");
    }

    [Test, Retry(2)]
    public async Task ShouldShowSameAssignedToMeCount_AsSearchPage()
    {
        var creator = await SeedEmployeeAsync();
        await SeedOrderForCurrentUserAsync(creator, WorkOrderStatus.Assigned);
        await SeedOrderForCurrentUserAsync(creator, WorkOrderStatus.InProgress);
        await SeedOrderForCurrentUserAsync(creator, WorkOrderStatus.Complete);

        await OpenDashboardAsync();
        var card = Page.GetByTestId(nameof(WorkOrderSummaryCards.Elements.SummaryCardAssignedToMe));
        await Expect(card.Locator(".summary-card-value")).ToHaveTextAsync("2");

        await card.ClickAsync();
        await Page.WaitForURLAsync(new Regex("AssignedToMe=true&OpenOnly=true"));
        await Expect(Page.GetByTestId(nameof(WorkOrderSummaryCards.Elements.SummaryCardAssignedToMe))
            .Locator(".summary-card-value")).ToHaveTextAsync("2");
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill))).ToHaveTextAsync("2 of 2");
    }

    [Test, Retry(2)]
    public async Task ShouldShowDueThisWeekCount_MatchingTheSearchItOpens()
    {
        var today = ChurchTimeZone.Today(TimeProvider.System);
        var creator = await SeedEmployeeAsync();
        await SeedOrderForCurrentUserAsync(creator, WorkOrderStatus.Assigned, dueDate: today);
        await SeedOrderForCurrentUserAsync(creator, WorkOrderStatus.Assigned, dueDate: today.AddDays(7));

        await OpenDashboardAsync();
        var card = Page.GetByTestId(nameof(WorkOrderSummaryCards.Elements.SummaryCardDueThisWeek));
        await Expect(card).ToBeVisibleAsync();
        var count = (await card.Locator(".summary-card-value").InnerTextAsync()).Trim();

        await card.ClickAsync();
        await Page.WaitForURLAsync(new Regex("DueThisWeekOnly=true"));
        await Expect(Page.GetByTestId(nameof(WorkOrderSummaryCards.Elements.SummaryCardDueThisWeek))
            .Locator(".summary-card-value")).ToHaveTextAsync(count);
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill)))
            .ToHaveTextAsync(new Regex("^\\d+ of " + Regex.Escape(count) + "$"));
    }

    [Test, Retry(2)]
    public async Task ShouldOpenSearchFilteredByStatus_WhenStatusCardClicked()
    {
        await OpenDashboardAsync();

        await Page.GetByTestId(nameof(IndexPage.Elements.StatusCard) + WorkOrderStatus.Cancelled.Key).ClickAsync();

        await Page.WaitForURLAsync(new Regex("workorder/search\\?Status=Cancelled"));
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.StatusSelect}")).ToHaveValueAsync(WorkOrderStatus.Cancelled.Key);
    }

    [Test, Retry(2)]
    public async Task ShouldListJustCompletedWorkOrder_InRecentActivity()
    {
        var creator = await SeedEmployeeAsync();
        var order = await SeedWorkOrderAsync(creator, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] recent complete",
            AssigneeUserName = CurrentUser.UserName,
            CompletedDate = DateTime.UtcNow
        });

        await OpenDashboardAsync();
        await Expect(Page.GetByTestId(nameof(IndexPage.Elements.RecentActivity))).ToBeVisibleAsync();

        // Facility-wide top-N can push peers off under ParallelScope.All; prove the seeded
        // complete via TestTag search, then open manage from that row.
        await Page.EvaluateAsync("u => Blazor.navigateTo(u)", $"/workorder/search?Q={TestTag}");
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.ResultsCountPill)))
            .ToHaveTextAsync(new Regex("1 of 1"));
        await Page.GetByRole(AriaRole.Link, new() { Name = order.Number! }).ClickAsync();
        await Page.WaitForURLAsync($"**/workorder/manage/{order.Number}?mode=Edit");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber))).ToHaveTextAsync(order.Number!);
    }

    [Test, Retry(2)]
    public async Task ShouldOpenMyQueue_FromQuickLink()
    {
        await OpenDashboardAsync();

        await Page.GetByTestId(nameof(IndexPage.Elements.QuickLinkMyQueue)).ClickAsync();

        await Page.WaitForURLAsync(new Regex("workorder/search\\?AssignedToMe=true&OpenOnly=true"));
        await Expect(Page.Locator($"#{WorkOrderSearch.Elements.AssignedToMeCheckbox}")).ToBeCheckedAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldKeepCardAndButtonColours_InDarkMode()
    {
        await OpenDashboardAsync();

        await Click(nameof(MainLayout.Elements.DarkModeToggle));
        await Page.WaitForFunctionAsync("() => document.documentElement.getAttribute('data-theme') === 'dark'");

        await Expect(Page.Locator(".summary-card").First).ToHaveCSSAsync("background-color", "rgb(21, 31, 51)");
        var link = Page.Locator("a.summary-card").First;
        await link.HoverAsync();
        await Expect(link).ToHaveCSSAsync("color", "rgb(226, 232, 240)");
        var newRequest = Page.GetByTestId(nameof(IndexPage.Elements.QuickLinkNewRequest));
        await Expect(newRequest).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await newRequest.HoverAsync();
        await Expect(newRequest).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        await Expect(Page.GetByTestId(nameof(IndexPage.Elements.QuickLinkWorkOrders)))
            .ToHaveCSSAsync("color", "rgb(226, 232, 240)");

        await Page.GetByTestId(nameof(IndexPage.Elements.QuickLinkWorkOrders)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/search");
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.NewWorkOrderButton)))
            .ToHaveCSSAsync("color", "rgb(255, 255, 255)");
    }

    [Test, Retry(2)]
    public async Task ShouldFillContentArea_WhenSignedOut_At1000Px()
    {
        await SetViewportAndSettleAsync(1000, 800);
        await Expect(Page.GetByTestId(nameof(IndexPage.Elements.SignInPrompt))).ToBeVisibleAsync();

        var content = await ContentBoxAsync();
        var header = await Page.Locator(".page-header").BoundingBoxAsync();

        ((double)header!.X).ShouldBe(content.Left, 1);
        ((double)header.Width).ShouldBe(content.Width, 1);
    }

    [TestCase(1000)]
    [TestCase(1280)]
    [Retry(2)]
    public async Task ShouldAlignHeaderWithSearchPage_WhenSignedIn(int width)
    {
        await SetViewportAndSettleAsync(width, 800);
        await OpenDashboardAsync();
        await WaitForLayoutSettledAsync();
        var content = await ContentBoxAsync();
        var dashboardHeader = await Page.Locator(".page-header").BoundingBoxAsync();

        await Page.GetByTestId(nameof(IndexPage.Elements.QuickLinkWorkOrders)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/search");
        var searchTable = Page.Locator(".grid-data tbody");
        await searchTable.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await WaitForLayoutSettledAsync();
        var searchHeader = await Page.Locator(".page-header").BoundingBoxAsync();

        dashboardHeader!.X.ShouldBe(searchHeader!.X, 1);
        dashboardHeader.Width.ShouldBe(searchHeader.Width, 1);
        ((double)dashboardHeader.Width).ShouldBe(content.Width, 1);
    }

    private async Task SetViewportAndSettleAsync(int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await Page.WaitForFunctionAsync(
            "([w, h]) => window.innerWidth === w && window.innerHeight === h" +
            " && document.getAnimations().every(a => !(a instanceof CSSTransition) || a.playState !== 'running')",
            new object[] { width, height });
    }

    private async Task WaitForLayoutSettledAsync()
    {
        await Page.WaitForFunctionAsync(
            "() => document.getAnimations().every(a => !(a instanceof CSSTransition) || a.playState !== 'running')");
    }

    private async Task<(double Left, double Width)> ContentBoxAsync()
    {
        var box = await Page.Locator(".content-wrapper").EvaluateAsync<double[]>(
            "e => { const r = e.getBoundingClientRect(); const s = getComputedStyle(e);" +
            " const l = parseFloat(s.paddingLeft), p = l + parseFloat(s.paddingRight);" +
            " return [r.left + l, e.clientWidth - p]; }");
        return (box[0], box[1]);
    }

    private async Task OpenDashboardAsync()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/");
        await Expect(Page.GetByTestId(nameof(IndexPage.Elements.StatusDashboard))).ToBeVisibleAsync();
    }

    private Task<WorkOrder> SeedOrderForCurrentUserAsync(
        Employee creator,
        WorkOrderStatus status,
        DateOnly? dueDate = null) =>
        SeedWorkOrderAsync(creator, status, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] dashboard {status.FriendlyName}",
            AssigneeUserName = CurrentUser.UserName,
            DueDate = dueDate,
            CompletedDate = status == WorkOrderStatus.Complete ? DateTime.UtcNow : null
        });
}
