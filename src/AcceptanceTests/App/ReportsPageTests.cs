using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
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

        var exportButton = Page.GetByTestId(nameof(Reports.Elements.ExportButton));
        await Expect(exportButton).ToBeEnabledAsync();
    }

    [Test, Retry(2)]
    public async Task Reports_AverageDaysToClose_ShouldMatchSearchPage()
    {
        // Both queries share one clock that is later than every stored work order date, so their
        // 30-day completion window holds only the two orders this test completes.
        var clock = TestData.FutureClock();
        var now = clock.GetUtcNow().UtcDateTime;
        var tag = TestData.Tag();
        var userName = TestData.UserName("rpavg");
        var employee = new Employee(userName, "Reports", "Average", TestData.Email(userName));
        WorkOrder[] orders =
        [
            new()
            {
                Creator = employee,
                Number = TestData.Number("R"),
                Title = $"[{tag}] avg",
                Status = WorkOrderStatus.Complete,
                CreatedDate = now.AddDays(-10),
                CompletedDate = now.AddDays(-3)
            },
            new()
            {
                Creator = employee,
                Number = TestData.Number("R"),
                Title = $"[{tag}] avg",
                Status = WorkOrderStatus.Complete,
                CreatedDate = now.AddDays(-8),
                CompletedDate = now.AddDays(-2)
            }
        ];

        await using (var seed = TestHost.NewDbContext())
        {
            seed.Add(employee);
            seed.AddRange(orders.Cast<object>());
            await seed.SaveChangesAsync();
        }

        try
        {
            await using var ctx = TestHost.NewDbContext();
            var reportsData = await new WorkOrderReportsQueryHandler(ctx, clock).Handle(new WorkOrderReportsQuery());
            var searchSummary = await new WorkOrderSearchSummaryQueryHandler(ctx, clock)
                .Handle(new WorkOrderSearchSummaryQuery(userName));

            // 7 and 6 days to close average to 6.5 on both pages
            reportsData.AverageDaysToClose.ShouldNotBeNull();
            searchSummary.AverageDaysToClose.ShouldNotBeNull();
            reportsData.AverageDaysToClose.Value.ShouldBe(6.5, 0.0001);
            searchSummary.AverageDaysToClose.Value.ShouldBe(6.5, 0.0001);
            reportsData.AverageDaysToClose.ShouldBe(searchSummary.AverageDaysToClose);
            reportsData.CompletedLast30Days.ShouldBe(2);
            searchSummary.CompletedLast30Days.ShouldBe(2);
        }
        finally
        {
            // Dates later than now must not outlive this test: unscoped pages (search, recent activity)
            // sort newest first, so these rows would push every other test's rows off the first page.
            await using var cleanup = TestHost.NewDbContext();
            foreach (var order in orders)
            {
                var stored = await cleanup.Set<WorkOrder>().FindAsync(order.Id);
                if (stored != null)
                {
                    cleanup.Remove(stored);
                }
            }

            await cleanup.SaveChangesAsync();
        }
    }

    [Test, Retry(2)]
    public async Task Reports_NavLink_ShouldNotHaveSoonTag()
    {
        await LoginAsCurrentUser();

        // Verify the Reports link does not have a "Soon" tag
        var reportsLink = Page.GetByTestId(nameof(NavMenu.Elements.Reports));
        await Expect(reportsLink).ToBeVisibleAsync();

        var soonTag = reportsLink.Locator(".nav-tag");
        await Expect(soonTag).Not.ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Reports_DownloadCsv_ShouldDownloadFile()
    {
        var (building, openCount) = await SeedLeadingBuilding();
        try
        {
            await LoginAsCurrentUser();
            await Click(nameof(NavMenu.Elements.Reports));

            // This test's building has the highest open count in the database, so it is first in the
            // Open by Building card (top 6) and in the CSV (top 10) whatever other tests left behind.
            await Expect(Page.GetByTestId(nameof(Reports.Elements.SummaryCardBuilding)))
                .ToContainTextAsync(building);

            // Wait for export button to be enabled before clicking
            await Expect(Page.GetByTestId(nameof(Reports.Elements.ExportButton))).ToBeEnabledAsync(new() { Timeout = 30_000 });

            // Initiate download and verify it completes
            var download = await Page.RunAndWaitForDownloadAsync(async () =>
                await Click(nameof(Reports.Elements.ExportButton)));

            var fileName = download.SuggestedFilename;

            // Verify filename matches expected pattern
            fileName.ShouldStartWith("reports-");
            fileName.ShouldEndWith(".csv");

            // Read and verify file content
            var filePath = await download.PathAsync();
            var content = await File.ReadAllTextAsync(filePath);
            var lines = content.Split("\r\n");

            // Verify header rows exist
            lines.ShouldContain("Category,Status,Count");
            lines.ShouldContain("Category,Priority,Count");
            lines.ShouldContain("Category,Building,Count");
            lines.ShouldContain("Week Ending,Completed");
            lines.ShouldContain("Metric,Value");

            // Verify summary metrics exist
            var completedLine = lines.FirstOrDefault(l => l.StartsWith("Completed (30 days),"));
            completedLine.ShouldNotBeNull();

            var avgLine = lines.FirstOrDefault(l => l.StartsWith("Average Days to Close,"));
            avgLine.ShouldNotBeNull();

            var overdueLineIndex = Array.FindIndex(lines, l => l.StartsWith("Overdue Count,"));
            overdueLineIndex.ShouldBeGreaterThanOrEqualTo(0);

            // The building row counts this test's open orders only, not its completed one
            lines.ShouldContain($"Open by Building,{building},{openCount}");

            try
            {
                await download.DeleteAsync();
            }
            catch
            {
                // Download cleanup is best-effort
            }
        }
        finally
        {
            // Rows of this test must not outlive it: on a reused database they would push later runs
            // down the top-N building lists.
            await using var cleanup = TestHost.NewDbContext();
            var seeded = await cleanup.Set<WorkOrder>().Where(wo => wo.Building == building).ToListAsync();
            cleanup.RemoveRange(seeded);
            await cleanup.SaveChangesAsync();
        }
    }

    private async Task<(string Building, int OpenCount)> SeedLeadingBuilding()
    {
        int highest;
        await using (var read = TestHost.NewDbContext())
        {
            var data = await new WorkOrderReportsQueryHandler(read, TimeProvider.System).Handle(new WorkOrderReportsQuery());
            highest = data.CountByBuilding.Values.DefaultIfEmpty(0).Max();
        }

        // Above every building already stored, and above the 3 orders a parallel test puts in one building
        var openCount = Math.Max(highest, 3) + 1;
        var building = $"Download {TestTag}";
        var userName = TestData.UserName("rd");
        var creator = new Employee(userName, "Reports", "Download", TestData.Email(userName));
        WorkOrderStatus[] openStatuses = [WorkOrderStatus.Draft, WorkOrderStatus.Assigned, WorkOrderStatus.InProgress];
        var now = DateTime.UtcNow;

        var orders = Enumerable.Range(0, openCount)
            .Select(i => new WorkOrder
            {
                Creator = creator,
                Number = TestData.Number("D"),
                Title = $"[{TestTag}] Download {i + 1}",
                Status = openStatuses[i % openStatuses.Length],
                Priority = WorkOrderPriority.Medium,
                Building = building,
                CreatedDate = now.AddDays(-3)
            })
            .Append(new WorkOrder
            {
                // A completed order in the same building must not be counted as open
                Creator = creator,
                Number = TestData.Number("D"),
                Title = $"[{TestTag}] Download done",
                Status = WorkOrderStatus.Complete,
                Priority = WorkOrderPriority.Medium,
                Building = building,
                CreatedDate = now.AddDays(-6),
                CompletedDate = now.AddDays(-2)
            })
            .ToArray();

        await using var context = TestHost.NewDbContext();
        context.Add(creator);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
        return (building, openCount);
    }

    private async Task SeedData()
    {
        // Seed this test's own rows for the Reports page: unique numbers, titles, creator and buildings
        await using var context = TestHost.NewDbContext();
        var userName = TestData.UserName("rc");
        var creator = new Employee(userName, "Reports", "Creator", TestData.Email(userName));
        var numbers = TestData.NumberSeries(5);
        var sanctuary = $"Sanctuary {TestTag}";
        var kitchen = $"Kitchen {TestTag}";
        var now = DateTime.UtcNow;

        WorkOrder[] orders =
        [
            new()
            {
                Creator = creator,
                Number = numbers[0],
                Title = $"[{TestTag}] Reports 1",
                Status = WorkOrderStatus.Draft,
                Priority = WorkOrderPriority.High,
                Building = sanctuary,
                CreatedDate = now.AddDays(-5),
                DueDate = DateOnly.FromDateTime(now.AddDays(-1))
            },
            new()
            {
                Creator = creator,
                Number = numbers[1],
                Title = $"[{TestTag}] Reports 2",
                Status = WorkOrderStatus.Assigned,
                Priority = WorkOrderPriority.Medium,
                Building = kitchen,
                CreatedDate = now.AddDays(-10)
            },
            new()
            {
                Creator = creator,
                Number = numbers[2],
                Title = $"[{TestTag}] Reports 3",
                Status = WorkOrderStatus.InProgress,
                Priority = WorkOrderPriority.Low,
                Building = sanctuary,
                CreatedDate = now.AddDays(-3)
            },
            new()
            {
                Creator = creator,
                Number = numbers[3],
                Title = $"[{TestTag}] Reports 4",
                Status = WorkOrderStatus.Complete,
                Priority = WorkOrderPriority.High,
                Building = sanctuary,
                CreatedDate = now.AddDays(-12),
                CompletedDate = now.AddDays(-5)
            },
            new()
            {
                Creator = creator,
                Number = numbers[4],
                Title = $"[{TestTag}] Reports 5",
                Status = WorkOrderStatus.Complete,
                Priority = WorkOrderPriority.Medium,
                Building = kitchen,
                CreatedDate = now.AddDays(-8),
                CompletedDate = now.AddDays(-2)
            }
        ];

        context.Add(creator);
        context.AddRange(orders.Cast<object>());
        await context.SaveChangesAsync();
    }
}
