using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class ReportsPageTests : AppAcceptanceTestBase
{
    [Test, Retry(2)]
    [Category("Smoke")]
    public async Task Reports_NavLink_ShouldOpenReportsPage()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));
        await Page.WaitForURLAsync("**/reports");

        var title = Page.GetByTestId(nameof(Reports.Elements.ReportsTitle));
        await Expect(title).ToContainTextAsync("Reports");

        var subtitle = Page.GetByTestId(nameof(Reports.Elements.ReportsSubtitle));
        await Expect(subtitle).ToContainTextAsync("Facility work order figures");
    }

    [Test, Retry(2)]
    public async Task Reports_ShouldRenderAllSummaryCards()
    {
        await SeedTaggedReportsDataAsync();
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));

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
        await SeedTaggedReportsDataAsync();
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));

        var weeklyChart = Page.GetByTestId(nameof(Reports.Elements.ChartCompletedPerWeek));
        await Expect(weeklyChart).ToBeVisibleAsync();
        await Expect(weeklyChart).ToContainTextAsync("Completed per week");

        var kpiCard = Page.GetByTestId(nameof(Reports.Elements.KpiOverdue));
        await Expect(kpiCard).ToBeVisibleAsync();
        await Expect(kpiCard).ToContainTextAsync("Overdue now");
    }

    [Test, Retry(2)]
    public async Task Reports_ExportButton_ShouldBeEnabled()
    {
        await SeedTaggedReportsDataAsync();
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));

        var exportButton = Page.GetByTestId(nameof(Reports.Elements.ExportButton));
        await Expect(exportButton).ToBeEnabledAsync();
    }

    [Test, Retry(2)]
    public async Task Reports_AverageDaysToClose_ShouldShowKpiAfterTaggedCompletes()
    {
        // Exact averaging math is covered by WorkOrderReportsQueryHandlerTests.
        // This acceptance case seeds tagged completes via API and proves the KPI renders.
        var now = DateTime.UtcNow;
        var creator = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] avg a",
            CreatedDate = now.AddDays(-10),
            CompletedDate = now.AddDays(-3)
        });
        await SeedWorkOrderAsync(creator, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] avg b",
            CreatedDate = now.AddDays(-8),
            CompletedDate = now.AddDays(-2)
        });

        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));

        var completedCard = Page.GetByTestId(nameof(Reports.Elements.SummaryCardCompletedLast30));
        await Expect(completedCard).ToBeVisibleAsync();
        await Expect(completedCard).ToContainTextAsync("Completed (30 days)");
        await Expect(Page.GetByText("days to close", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Reports_NavLink_ShouldNotHaveSoonTag()
    {
        await LoginAsCurrentUser();

        var reportsLink = Page.GetByTestId(nameof(NavMenu.Elements.Reports));
        await Expect(reportsLink).ToBeVisibleAsync();

        var soonTag = reportsLink.Locator(".nav-tag");
        await Expect(soonTag).Not.ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Reports_DownloadCsv_ShouldDownloadFile()
    {
        var building = $"Download {TestTag}";
        await SeedTaggedBuildingOrdersAsync(building);
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Reports));
        await Expect(Page.GetByTestId(nameof(Reports.Elements.SummaryCardBuilding))).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(Reports.Elements.ExportButton)))
            .ToBeEnabledAsync(new() { Timeout = 30_000 });
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
            await Click(nameof(Reports.Elements.ExportButton)));
        await AssertReportsCsvHeadersAsync(download);
    }

    private async Task SeedTaggedBuildingOrdersAsync(string building)
    {
        var creator = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, WorkOrderStatus.Draft, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] Download open",
            Building = building
        });
        await SeedWorkOrderAsync(creator, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] Download done",
            Building = building,
            CompletedDate = DateTime.UtcNow.AddDays(-2)
        });
    }

    private static async Task AssertReportsCsvHeadersAsync(IDownload download)
    {
        download.SuggestedFilename.ShouldStartWith("reports-");
        download.SuggestedFilename.ShouldEndWith(".csv");
        var lines = (await File.ReadAllTextAsync(await download.PathAsync())).Split("\r\n");
        lines.ShouldContain("Category,Status,Count");
        lines.ShouldContain("Category,Priority,Count");
        lines.ShouldContain("Category,Building,Count");
        lines.ShouldContain("Week Ending,Completed");
        lines.ShouldContain("Metric,Value");
        lines.FirstOrDefault(l => l.StartsWith("Completed (30 days),")).ShouldNotBeNull();
        lines.FirstOrDefault(l => l.StartsWith("Average Days to Close,")).ShouldNotBeNull();
        Array.FindIndex(lines, l => l.StartsWith("Overdue Count,")).ShouldBeGreaterThanOrEqualTo(0);
        try { await download.DeleteAsync(); }
        catch { /* best-effort */ }
    }

    private async Task SeedTaggedReportsDataAsync()
    {
        var creator = await SeedEmployeeAsync();
        await SeedWorkOrderAsync(creator, WorkOrderStatus.Draft, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] Reports open",
            Priority = WorkOrderPriority.High,
            Building = $"Sanctuary {TestTag}",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        });
        await SeedWorkOrderAsync(creator, WorkOrderStatus.Complete, new WorkOrderSeedFields
        {
            Title = $"[{TestTag}] Reports done",
            Building = $"Kitchen {TestTag}",
            CreatedDate = DateTime.UtcNow.AddDays(-8),
            CompletedDate = DateTime.UtcNow.AddDays(-2)
        });
    }
}
