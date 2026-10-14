using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class ReportsPageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    [Category("Smoke")]
    public async Task Reports_NavLink_ShouldOpenReportsPage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));
        await Page.WaitForURLAsync("**/reports");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify page title
        var title = Page.GetByTestId(nameof(Reports.Elements.ReportsTitle));
        await Expect(title).ToContainTextAsync("Reports");

        // Verify subtitle
        var subtitle = Page.GetByTestId(nameof(Reports.Elements.ReportsSubtitle));
        await Expect(subtitle).ToContainTextAsync("Facility work order figures");
    }

    [Test, Retry(2)]
    public async Task Reports_ShouldRenderAllSummaryCards()
    {
        await SeedData();
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify all 4 cards are visible
        var statusCard = Page.GetByTestId(nameof(Reports.Elements.SummaryCardStatus));
        await Expect(statusCard).ToBeVisibleAsync();
        await Expect(statusCard).ToContainTextAsync("Open by Status");

        var priorityCard = Page.GetByTestId(nameof(Reports.Elements.SummaryCardPriority));
        await Expect(priorityCard).ToBeVisibleAsync();
        await Expect(priorityCard).ToContainTextAsync("Open by Priority");

        var buildingCard = Page.GetByTestId(nameof(Reports.Elements.SummaryCardBuilding));
        await Expect(buildingCard).ToBeVisibleAsync();
        await Expect(buildingCard).ToContainTextAsync("Open by Building");

        var completedCard = Page.GetByTestId(nameof(Reports.Elements.SummaryCardCompletedLast30));
        await Expect(completedCard).ToBeVisibleAsync();
        await Expect(completedCard).ToContainTextAsync("Completed (30 days)");
    }

    [Test, Retry(2)]
    public async Task Reports_ShouldRenderCharts()
    {
        await SeedData();
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify weekly completions chart is visible
        var weeklyChart = Page.GetByTestId(nameof(Reports.Elements.ChartCompletedPerWeek));
        await Expect(weeklyChart).ToBeVisibleAsync();
        await Expect(weeklyChart).ToContainTextAsync("Completed per week");

        // Verify KPI card is visible
        var kpiCard = Page.GetByTestId(nameof(Reports.Elements.KpiOverdue));
        await Expect(kpiCard).ToBeVisibleAsync();
        await Expect(kpiCard).ToContainTextAsync("Overdue now");
    }

    [Test, Retry(2)]
    public async Task Reports_ExportButton_ShouldBeEnabled()
    {
        await SeedData();
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var exportButton = Page.GetByTestId(nameof(Reports.Elements.ExportButton));
        await Expect(exportButton).ToBeEnabledAsync();
    }

    [Test, Retry(2)]
    public async Task Reports_AverageDaysToClose_ShouldMatchSearchPage()
    {
        // Verify that the Reports query result agrees with the Search page's summary KPI
        await SeedData();
        await LoginAsCurrentUser();

        // Get reports data
        var reportsQuery = new WorkOrderReportsQuery();
        var reportsData = await Bus.Send(reportsQuery);

        // Get search summary data
        var searchQuery = new WorkOrderSearchSummaryQuery(CurrentUser.UserName);
        var searchSummary = await Bus.Send(searchQuery);

        // Both should have the same average days to close
        reportsData.AverageDaysToClose.ShouldBe(searchSummary.AverageDaysToClose);
    }

    [Test, Retry(2)]
    public async Task Reports_NavLink_ShouldNotHaveSoonTag()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify the Reports link does not have a "Soon" tag
        var reportsLink = Page.GetByTestId(nameof(NavMenu.Elements.Reports));
        await Expect(reportsLink).ToBeVisibleAsync();

        var soonTag = reportsLink.Locator(".nav-tag");
        await Expect(soonTag).Not.ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Reports_DownloadCsv_ShouldDownloadFile()
    {
        await SeedData();
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Initiate download and verify it completes
        var downloadTask = Page.WaitForDownloadAsync();

        await Click(nameof(Reports.Elements.ExportButton));

        var download = await downloadTask;
        var fileName = download.SuggestedFilename;

        // Verify filename matches expected pattern
        fileName.ShouldStartWith("reports-");
        fileName.ShouldEndWith(".csv");

        // Cleanup
        await download.DeleteAsync();
    }

    private async Task SeedData()
    {
        // Seed test data for the Reports page
        await using var context = TestHost.GetRequiredService<DataContext>();
        var tag = Guid.NewGuid().ToString("N")[..5];
        var creator = new Employee("rc" + tag, "Reports", "Creator", "rc" + tag + "@test.com");
        var now = DateTime.UtcNow;

        WorkOrder[] orders =
        [
            new WorkOrder
            {
                Creator = creator,
                Number = $"R{tag}1",
                Title = "Test 1",
                Status = WorkOrderStatus.Draft,
                Priority = WorkOrderPriority.High,
                Building = "Sanctuary",
                CreatedDate = now.AddDays(-5),
                DueDate = DateOnly.FromDateTime(now.AddDays(-1))
            },
            new WorkOrder
            {
                Creator = creator,
                Number = $"R{tag}2",
                Title = "Test 2",
                Status = WorkOrderStatus.Assigned,
                Priority = WorkOrderPriority.Medium,
                Building = "Kitchen",
                CreatedDate = now.AddDays(-10)
            },
            new WorkOrder
            {
                Creator = creator,
                Number = $"R{tag}3",
                Title = "Test 3",
                Status = WorkOrderStatus.InProgress,
                Priority = WorkOrderPriority.Low,
                Building = "Sanctuary",
                CreatedDate = now.AddDays(-3)
            },
            new WorkOrder
            {
                Creator = creator,
                Number = $"R{tag}4",
                Title = "Test 4",
                Status = WorkOrderStatus.Complete,
                Priority = WorkOrderPriority.High,
                Building = "Sanctuary",
                CreatedDate = now.AddDays(-12),
                CompletedDate = now.AddDays(-5)
            },
            new WorkOrder
            {
                Creator = creator,
                Number = $"R{tag}5",
                Title = "Test 5",
                Status = WorkOrderStatus.Complete,
                Priority = WorkOrderPriority.Medium,
                Building = "Kitchen",
                CreatedDate = now.AddDays(-8),
                CompletedDate = now.AddDays(-2)
            }
        ];

        context.Add(creator);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }
}
