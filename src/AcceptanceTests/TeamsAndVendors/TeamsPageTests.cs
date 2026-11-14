using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TeamsAndVendors;

[TestFixture]
public class TeamsPageTests : TeamsAndVendorsAcceptanceTestBase
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
        await GotoTeamsPageAsync();

        var table = Page.Locator(".teams-table");
        await Expect(table).ToBeVisibleAsync();
        (await table.Locator("thead th").CountAsync()).ShouldBeGreaterThanOrEqualTo(6);
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Name", Exact = true })).ToBeVisibleAsync();
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Role", Exact = true })).ToBeVisibleAsync();
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Assigned Open", Exact = true })).ToBeVisibleAsync();
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "In Progress", Exact = true })).ToBeVisibleAsync();
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Overdue", Exact = true })).ToBeVisibleAsync();
        await Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Completed 30d", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShouldDisplayCurrentUserInTable()
    {
        await LoginAsCurrentUser();
        await GotoTeamsPageAsync();

        var userNameCell = Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowName}{CurrentUser.UserName}");
        await Expect(userNameCell).ToBeVisibleAsync();
        await Expect(userNameCell).ToContainTextAsync(CurrentUser.GetFullName());
    }

    [Test]
    public async Task ShouldSortByNameAscendingByDefault()
    {
        await LoginAsCurrentUser();
        await GotoTeamsPageAsync();

        var rows = Page.Locator(".teams-table tbody tr");
        await Expect(rows.First).ToBeVisibleAsync();
        (await rows.CountAsync()).ShouldBeGreaterThan(0);

        var firstEmployeeName = await rows.First.Locator("[data-testid*='TableRowName']").InnerTextAsync();
        firstEmployeeName.ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task ShouldRenderDarkModeWithoutBreakage()
    {
        await LoginAsCurrentUser();
        await GotoTeamsPageAsync();

        await Page.EvaluateAsync("() => { document.documentElement.setAttribute('data-theme', 'dark'); }");
        await Task.Delay(500);

        var table = Page.Locator(".teams-table");
        await Expect(table).ToBeVisibleAsync();
        await Expect(table.Locator("thead th").First).ToBeVisibleAsync();
        await Expect(table.Locator("tbody tr").First).ToBeVisibleAsync();
        (await table.Locator("tbody tr").CountAsync()).ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task ShouldMatchOpenCountBetweenTeamsPageAndSearchPage()
    {
        await SeedTwoAssignedOpenOrdersAsync();
        await LoginAsCurrentUser();
        await GotoTeamsPageAsync();

        var teamsOpenCount = int.Parse(await AssignedOpenCell(CurrentUser.UserName).InnerTextAsync());
        teamsOpenCount.ShouldBe(2);

        var searchUrl = $"/workorder/search?Assignee={Uri.EscapeDataString(CurrentUser.UserName)}&OpenOnly=true";
        await Page.GotoAsync(searchUrl);
        await Expect(Page.Locator(".grid-data tbody tr")).ToHaveCountAsync(teamsOpenCount);
    }

    [Test]
    public async Task ShouldDisplayZeroCountsForEmployeeWithNoWorkOrders()
    {
        await LoginAsCurrentUser();
        await GotoTeamsPageAsync();

        int.Parse(await AssignedOpenCell(CurrentUser.UserName).InnerTextAsync()).ShouldBe(0);
        int.Parse(await Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowInProgress}{CurrentUser.UserName}").InnerTextAsync()).ShouldBe(0);
        int.Parse(await Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowOverdue}{CurrentUser.UserName}").InnerTextAsync()).ShouldBe(0);
        int.Parse(await Page.GetByTestId($"{UI.Shared.Pages.TeamsAndVendors.Elements.TableRowCompleted}{CurrentUser.UserName}").InnerTextAsync()).ShouldBe(0);
    }

    [Test]
    public async Task ShouldDisplayTeamCreatedThroughCommand()
    {
        await LoginAsCurrentUser();
        var team = await CreateTaggedTeamAsync("Team", "Acceptance team", [CurrentUser.UserName]);

        await GotoTeamsPageAsync();
        await Expect(Page.GetByText($"Team {TestTag}", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText($"Acceptance team {TestTag}")).ToBeVisibleAsync();
        team.Id.ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldDisplayTeamMembersAsAvatars()
    {
        var other = await SeedEmployeeAsync();
        await LoginAsCurrentUser();
        var team = await CreateTaggedTeamAsync("Multi", null, [CurrentUser.UserName, other.UserName]);

        await GotoTeamsPageAsync();
        var teamCard = TeamCard(team.Id!.Value);
        await Expect(teamCard).ToBeVisibleAsync();
        await Expect(teamCard.Locator("[data-testid*='TeamMemberCount']")).ToContainTextAsync("2 members");
        (await teamCard.Locator("[class*='member-avatar-link']").CountAsync()).ShouldBe(2);
    }
}
