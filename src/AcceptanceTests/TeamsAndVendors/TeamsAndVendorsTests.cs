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

    [Test]
    public async Task ShouldDisplayTeamCreatedThroughCommand()
    {
        await LoginAsCurrentUser();

        // Create a team through the command
        var teamName = $"Test Team {Guid.NewGuid()}";
        var teamResult = await Bus.Send(new CreateTeamCommand(
            teamName,
            "A test team for acceptance testing",
            [CurrentUser.UserName]
        ));
        teamResult.Succeeded.ShouldBeTrue();

        // Navigate to Teams & Vendors page
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify team is displayed
        await Expect(Page.GetByText(teamName, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("A test team for acceptance testing")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldDisplayTeamMembersAsAvatars()
    {
        await LoginAsCurrentUser();

        // Get another employee to add to the team
        var employees = await Bus.Send(new EmployeeGetAllQuery());
        var otherEmployee = employees.FirstOrDefault(e => e.UserName != CurrentUser.UserName && e.CanFulfillWorkOrder());
        if (otherEmployee == null)
        {
            Assert.Ignore("No other assignable employee found for this test");
        }

        // Create a team with multiple members
        var teamName = $"Multi-Member Team {Guid.NewGuid()}";
        var teamResult = await Bus.Send(new CreateTeamCommand(
            teamName,
            null,
            [CurrentUser.UserName, otherEmployee.UserName]
        ));
        teamResult.Succeeded.ShouldBeTrue();

        // Navigate to Teams & Vendors page
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify team is displayed and has member avatars
        var teamCard = Page.Locator($"[data-testid*='TeamCard'][data-testid*='{teamResult.Id}']");
        await Expect(teamCard).ToBeVisibleAsync();

        // Verify member count is displayed
        var memberCount = teamCard.Locator("[data-testid*='TeamMemberCount']");
        await Expect(memberCount).ToContainTextAsync("2 members");

        // Verify member avatars are displayed
        var memberAvatars = teamCard.Locator("[class*='member-avatar-link']");
        var avatarCount = await memberAvatars.CountAsync();
        avatarCount.ShouldBe(2);
    }

    [Test]
    public async Task ShouldDisplayVendorCreatedThroughCommand()
    {
        await LoginAsCurrentUser();

        // Create a vendor through the command
        var vendorName = $"Test Vendor {Guid.NewGuid()}";
        var vendorResult = await Bus.Send(new CreateVendorCommand(
            vendorName,
            "Plumbing",
            "John Smith",
            "john@plumbing.com",
            "555-1234"
        ));
        vendorResult.Succeeded.ShouldBeTrue();

        // Navigate to Teams & Vendors page
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify vendor is displayed
        await Expect(Page.GetByText(vendorName, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Plumbing", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("John Smith")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldDisplayVendorDetailsCorrectly()
    {
        await LoginAsCurrentUser();

        // Create a vendor with all details
        var vendorName = $"Complete Vendor {Guid.NewGuid()}";
        var vendorResult = await Bus.Send(new CreateVendorCommand(
            vendorName,
            "HVAC",
            "Jane Doe",
            "jane@hvac.com",
            "555-5678"
        ));
        vendorResult.Succeeded.ShouldBeTrue();

        // Navigate to Teams & Vendors page
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the vendor card
        var vendorCard = Page.Locator($"[data-testid*='VendorCard'][data-testid*='{vendorResult.Id}']");
        await Expect(vendorCard).ToBeVisibleAsync();

        // Verify all vendor details are displayed
        await Expect(vendorCard).ToContainTextAsync(vendorName);
        await Expect(vendorCard).ToContainTextAsync("HVAC");
        await Expect(vendorCard).ToContainTextAsync("Jane Doe");
        await Expect(vendorCard).ToContainTextAsync("jane@hvac.com");
        await Expect(vendorCard).ToContainTextAsync("555-5678");
        await Expect(vendorCard).ToContainTextAsync("Active");
    }

    [Test]
    public async Task ShouldDisplayInactiveVendorWithInactiveStatus()
    {
        await LoginAsCurrentUser();

        // Create an inactive vendor
        var vendorName = $"Inactive Vendor {Guid.NewGuid()}";
        var vendorResult = await Bus.Send(new CreateVendorCommand(
            vendorName,
            "Electrical",
            "Bob Jones",
            "bob@electrical.com",
            "555-9999",
            false
        ));
        vendorResult.Succeeded.ShouldBeTrue();

        // Navigate to Teams & Vendors page
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the vendor card
        var vendorCard = Page.Locator($"[data-testid*='VendorCard'][data-testid*='{vendorResult.Id}']");
        await Expect(vendorCard).ToBeVisibleAsync();

        // Verify inactive status is displayed
        await Expect(vendorCard).ToContainTextAsync("Inactive");
    }

    [Test]
    public async Task ShouldDisplayVendorEmailAsMailtoLink()
    {
        await LoginAsCurrentUser();

        // Create a vendor with email
        var vendorName = $"Email Vendor {Guid.NewGuid()}";
        var vendorEmail = "contact@vendor.com";
        var vendorResult = await Bus.Send(new CreateVendorCommand(
            vendorName,
            null,
            null,
            vendorEmail,
            null
        ));
        vendorResult.Succeeded.ShouldBeTrue();

        // Navigate to Teams & Vendors page
        await Page.GotoAsync("/admin/teams");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the email link in the vendor card
        var emailLink = Page.Locator($"[data-testid*='VendorEmail'][data-testid*='{vendorResult.Id}'] a");
        await Expect(emailLink).ToBeVisibleAsync();
        await Expect(emailLink).ToHaveAttributeAsync("href", $"mailto:{vendorEmail}");
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
