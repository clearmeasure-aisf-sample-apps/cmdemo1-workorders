using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class DashboardTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShowHeaderAndCards_WithoutHero()
    {
        await OpenDashboardAsync();

        await Expect(Page.Locator(".page-heading h1")).ToHaveTextAsync("TechWeek Work Orders");
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
        var creator = await SeedCreatorAsync("dash");
        await SeedOrderAsync(creator, WorkOrderStatus.Assigned, CurrentUser);
        await SeedOrderAsync(creator, WorkOrderStatus.InProgress, CurrentUser);
        await SeedOrderAsync(creator, WorkOrderStatus.Complete, CurrentUser);

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
        var creator = await SeedCreatorAsync("dueweek");
        await SeedOrderAsync(creator, WorkOrderStatus.Assigned, CurrentUser, dueDate: today);
        await SeedOrderAsync(creator, WorkOrderStatus.Assigned, CurrentUser, dueDate: today.AddDays(7));

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

    [Test, Retry(2), NonParallelizable]
    public async Task ShouldListJustCompletedWorkOrder_InRecentActivity()
    {
        var creator = await SeedCreatorAsync("recent");
        var order = await SeedOrderAsync(creator, WorkOrderStatus.Complete, CurrentUser, DateTime.UtcNow);

        await OpenDashboardAsync();
        var item = Page.GetByTestId(nameof(IndexPage.Elements.RecentActivity))
            .GetByTestId(nameof(IndexPage.Elements.RecentActivityItem) + order.Number);
        await Expect(item).ToBeVisibleAsync();
        await Expect(item.Locator(".activity-meta")).ToContainTextAsync("Completed");

        await Page.GetByTestId(nameof(IndexPage.Elements.RecentActivityLink) + order.Number).ClickAsync();
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

    private async Task<Employee> SeedCreatorAsync(string prefix)
    {
        var creator = Faker<Employee>();
        creator.UserName = $"{prefix}_{TestTag}";
        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        await context.SaveChangesAsync();
        return creator;
    }

    private async Task<WorkOrder> SeedOrderAsync(Employee creator, WorkOrderStatus status, Employee assignee,
        DateTime? completedDate = null, DateOnly? dueDate = null)
    {
        var order = Faker<WorkOrder>();
        order.Creator = creator;
        order.Assignee = assignee;
        order.Status = status;
        order.CreatedDate = DateTime.UtcNow.AddDays(-3);
        order.AssignedDate = DateTime.UtcNow.AddDays(-2);
        order.CompletedDate = completedDate;
        if (dueDate.HasValue)
        {
            order.DueDate = dueDate;
        }

        order.Title = $"[{TestTag}] dashboard {status.FriendlyName}";
        await using var context = TestHost.NewDbContext();
        context.Attach(creator);
        context.Attach(assignee);
        context.Add(order);
        await context.SaveChangesAsync();
        return order;
    }
}
