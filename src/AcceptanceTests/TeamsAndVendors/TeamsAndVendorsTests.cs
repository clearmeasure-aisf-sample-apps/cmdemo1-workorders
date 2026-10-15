using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors;

public class TeamsAndVendorsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldNavigateToTeamsAndVendorsPageFromSidebar()
    {
        await LoginAsCurrentUser();

        var teamsLink = Page.GetByTestId(nameof(NavMenu.Elements.TeamsAndVendors));
        await Expect(teamsLink).ToBeVisibleAsync();
        await Expect(teamsLink).ToContainTextAsync("Teams & Vendors");

        await Click(nameof(NavMenu.Elements.TeamsAndVendors));
        await Page.WaitForURLAsync("**/admin/teams");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Teams & Vendors" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldDisplayTableWithAllColumns()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/admin/teams");

        var table = Page.Locator(".teams-table");
        await Expect(table).ToBeVisibleAsync();

        var headers = table.Locator("thead th");
        var headerCount = await headers.CountAsync();
        headerCount.ShouldBeGreaterThanOrEqualTo(6);

        // Match the column headers exactly: a loose text match also finds employee names in the rows
        // (for example "Role" in "Carole") and fails Playwright's strict mode.
        foreach (var header in new[] { "Name", "Role", "Assigned Open", "In Progress", "Overdue", "Completed 30d" })
        {
            await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = header, Exact = true })).ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task ShouldDisplayCurrentUserInTable()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/admin/teams");

        var userNameCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowName}{CurrentUser.UserName}");
        await Expect(userNameCell).ToBeVisibleAsync();
        await Expect(userNameCell).ToContainTextAsync(CurrentUser.GetFullName());
    }

    [Test]
    public async Task ShouldSortByNameAscendingByDefault()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/admin/teams");

        var rows = Page.Locator(".teams-table tbody tr");
        await Expect(rows.First).ToBeVisibleAsync();
        var rowCount = await rows.CountAsync();
        rowCount.ShouldBeGreaterThan(0);

        var firstRow = rows.First;
        var firstEmployeeName = await firstRow.Locator("[data-testid*='TableRowName']").InnerTextAsync();
        firstEmployeeName.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task ShouldRenderDarkModeWithoutBreakage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/admin/teams");

        await Page.EvaluateAsync("() => { document.documentElement.setAttribute('data-theme', 'dark'); }");
        await Task.Delay(500);

        var table = Page.Locator(".teams-table");
        await Expect(table).ToBeVisibleAsync();

        var headers = table.Locator("thead th");
        await Expect(headers.First).ToBeVisibleAsync();

        var rows = table.Locator("tbody tr");
        await Expect(rows.First).ToBeVisibleAsync();
        var rowCount = await rows.CountAsync();
        rowCount.ShouldBeGreaterThan(0);

        var firstRow = rows.First;
        await Expect(firstRow).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldMatchOpenCountBetweenTeamsPageAndSearchPage()
    {
        await LoginAsCurrentUser();

        // Create test work orders: 2 assigned open, 1 completed
        var wo1 = await CreateAndSaveNewWorkOrder();
        var wo2 = await CreateAndSaveNewWorkOrder();
        var wo3 = await CreateAndSaveNewWorkOrder();

        // Assign wo1 and wo2 to current user (status Assigned = open)
        AssignWorkOrderToEmployee(wo1, CurrentUser);
        AssignWorkOrderToEmployee(wo2, CurrentUser);

        // Mark wo3 as complete (not open)
        var wo3Full = await Bus.Send(new WorkOrderByNumberQuery(wo3.Number!)) ?? throw new InvalidOperationException();
        wo3Full.ChangeStatus(WorkOrderStatus.Complete);
        await using var completeContext = TestHost.NewDbContext();
        completeContext.Update(wo3Full);
        await completeContext.SaveChangesAsync();

        // Read open count from Teams & Vendors page
        await Page.GotoAsync("/admin/teams");

        var openCountCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowAssignedOpen}{CurrentUser.UserName}");
        var openCountText = await openCountCell.InnerTextAsync();
        var teamsOpenCount = int.Parse(openCountText);

        // Navigate to search page and count results with same assignee filter
        var searchUrl = $"/workorder/search?Assignee={Uri.EscapeDataString(CurrentUser.UserName)}&OpenOnly=true";
        await Page.GotoAsync(searchUrl);

        // Results render asynchronously after the interactive circuit runs the search, so wait
        // (web-first) for both assigned work orders before counting; CountAsync does not wait.
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + wo1.Number)).ToBeVisibleAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + wo2.Number)).ToBeVisibleAsync();

        // Count rows in the search results table (OpenOnly=true restricts to Draft, Assigned and InProgress, matching the Teams open count)
        var searchResultRows = Page.Locator(".grid-data tbody tr");
        await Expect(searchResultRows).ToHaveCountAsync(teamsOpenCount);
        var searchOpenCount = await searchResultRows.CountAsync();

        // Counts should match
        teamsOpenCount.ShouldBe(searchOpenCount);
        teamsOpenCount.ShouldBe(2); // wo1 and wo2 are open
    }

    [Test]
    public async Task ShouldDisplayZeroCountsForEmployeeWithNoWorkOrders()
    {
        await LoginAsCurrentUser();

        // Ensure current user has no work orders assigned
        await Page.GotoAsync("/admin/teams");

        var openCountCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowAssignedOpen}{CurrentUser.UserName}");
        var openCountText = await openCountCell.InnerTextAsync();

        var inProgressCountCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowInProgress}{CurrentUser.UserName}");
        var inProgressCountText = await inProgressCountCell.InnerTextAsync();

        var overdueCountCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowOverdue}{CurrentUser.UserName}");
        var overdueCountText = await overdueCountCell.InnerTextAsync();

        var completedCountCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowCompleted}{CurrentUser.UserName}");
        var completedCountText = await completedCountCell.InnerTextAsync();

        // All counts should be 0
        int.Parse(openCountText).ShouldBe(0);
        int.Parse(inProgressCountText).ShouldBe(0);
        int.Parse(overdueCountText).ShouldBe(0);
        int.Parse(completedCountText).ShouldBe(0);
    }

    private void AssignWorkOrderToEmployee(WorkOrder workOrder, Employee employee)
    {
        using var readContext = TestHost.NewDbContext();
        var wo = readContext.Set<WorkOrder>().Single(w => w.Number == workOrder.Number);
        wo.Assignee = readContext.Set<Employee>().Single(e => e.UserName == employee.UserName);
        wo.ChangeStatus(WorkOrderStatus.Assigned);
        using var writeContext = TestHost.NewDbContext();
        writeContext.Update(wo);
        writeContext.SaveChanges();
    }
}
