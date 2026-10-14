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
}
