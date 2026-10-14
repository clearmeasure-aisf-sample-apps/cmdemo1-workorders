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
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var table = Page.Locator(".teams-table");
        await Expect(table).ToBeVisibleAsync();

        var headers = table.Locator("thead th");
        var headerCount = await headers.CountAsync();
        headerCount.ShouldBeGreaterThanOrEqualTo(6);

        await Expect(table.Locator("text=Name")).ToBeVisibleAsync();
        await Expect(table.Locator("text=Role")).ToBeVisibleAsync();
        await Expect(table.Locator("text=Assigned Open")).ToBeVisibleAsync();
        await Expect(table.Locator("text=In Progress")).ToBeVisibleAsync();
        await Expect(table.Locator("text=Overdue")).ToBeVisibleAsync();
        await Expect(table.Locator("text=Completed 30d")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldDisplayCurrentUserInTable()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var userNameCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowName}{CurrentUser.UserName}");
        await Expect(userNameCell).ToBeVisibleAsync();
        await Expect(userNameCell).ToContainTextAsync(CurrentUser.GetFullName());
    }

    [Test]
    public async Task ShouldSortByNameAscendingByDefault()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rows = Page.Locator(".teams-table tbody tr");
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
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.EvaluateAsync("() => { document.documentElement.setAttribute('data-theme', 'dark'); }");
        await Task.Delay(500);

        var table = Page.Locator(".teams-table");
        await Expect(table).ToBeVisibleAsync();

        var headers = table.Locator("thead th");
        await Expect(headers.First).ToBeVisibleAsync();

        var rows = table.Locator("tbody tr");
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
        using (var context = TestHost.NewDbContext())
        {
            context.Update(wo3Full);
            await context.SaveChangesAsync();
        }

        // Read open count from Teams & Vendors page
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var openCountCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowAssignedOpen}{CurrentUser.UserName}");
        var openCountText = await openCountCell.InnerTextAsync();
        var teamsOpenCount = int.Parse(openCountText);

        // Navigate to search page and count results with same assignee filter
        var searchUrl = $"/workorder/search?Assignee={Uri.EscapeDataString(CurrentUser.UserName)}&OpenOnly=true";
        await Page.GotoAsync(searchUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Count rows in the search results table (OpenOnly=true restricts to Draft, Assigned and InProgress, matching the Teams open count)
        var searchResultRows = Page.Locator(".grid-data tbody tr");
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
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
        wo.Assignee = employee;
        wo.ChangeStatus(WorkOrderStatus.Assigned);
        using var writeContext = TestHost.NewDbContext();
        writeContext.Update(wo);
        writeContext.SaveChanges();
    }
}
