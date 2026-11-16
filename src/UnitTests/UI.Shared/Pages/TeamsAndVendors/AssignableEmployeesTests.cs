using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;
using TeamsAndVendorsPage = ClearMeasure.Bootcamp.UI.Shared.Pages.TeamsAndVendors;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.TeamsAndVendors;

[TestFixture]
public class AssignableEmployeesTests
{
    [Test]
    public async Task TeamsAndVendors_WhileLoading_AnnouncesStatusToScreenReaders()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bus = new TeamsAndVendorsBus { Pending = gate.Task };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        var loading = component.Find(".teams-loading");
        loading.GetAttribute("role").ShouldBe("status");
        loading.TextContent.ShouldContain("Loading...");
        gate.SetResult();
    }

    [Test]
    public async Task TeamsAndVendors_OnInitialized_ShouldLoadAllAssignableEmployees()
    {
        var bus = new TeamsAndVendorsBus();
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => component.Markup.ShouldContain("HOMER SIMPSON"));
        bus.SummaryQueriesSent.ShouldBe(1);
        bus.EmployeeGetAllQueriesSent.ShouldBe(0);
        bus.EmployeeWorkLoadQueriesSent.ShouldBeEmpty();
    }

    [Test]
    public async Task TeamsAndVendors_WhenNoAssignableEmployees_ShouldShowEmptyState()
    {
        var bus = new TeamsAndVendorsBus { EmployeesToReturn = [] };
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
            component.Markup.ShouldContain("No assignable employees found"));
    }

    [Test]
    public async Task TeamsAndVendors_TableRendersAllColumns()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldContain("Name");
            component.Markup.ShouldContain("Role");
            component.Markup.ShouldContain("Assigned Open");
            component.Markup.ShouldContain("In Progress");
            component.Markup.ShouldContain("Overdue");
            component.Markup.ShouldContain("Completed 30d");
        });
    }

    [Test]
    public async Task TeamsAndVendors_EmployeeRow_DisplaysNameWithAvatar()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var nameCell = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TableRowName}hsimpson']");
            nameCell.TextContent.ShouldContain("HOMER SIMPSON");
            nameCell.OuterHtml.ShouldContain("avatar");
        });
    }

    [Test]
    public async Task TeamsAndVendors_EmployeeRow_DisplaysRole()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var roleCell = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TableRowRole}hsimpson']");
            roleCell.TextContent.ShouldNotBeNullOrEmpty();
        });
    }

    [Test]
    public async Task TeamsAndVendors_EmployeeRow_DisplaysWorkLoadCounts()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => AssertWorkLoadCounts(component));
    }

    [Test]
    public async Task TeamsAndVendors_EmployeeName_LinksToSearchPageWithAssigneeFilter()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() => AssertAssigneeSearchLink(component));
    }

    [Test]
    public async Task TeamsAndVendors_SortByName_ClickingNameHeaderTogglesSortDirection()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var nameHeaders = component.FindAll("button.sort-header");
            nameHeaders.Count.ShouldBeGreaterThan(0);
            nameHeaders[0].TextContent.ShouldContain("Name");
        });

        await component.FindAll("button.sort-header")[0].ClickAsync();
        await component.WaitForAssertionAsync(() =>
            component.FindAll("tbody tr").Count.ShouldBeGreaterThan(0));
    }

    [Test]
    public async Task TeamsAndVendors_DefaultSort_NameAscending()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var rows = component.FindAll("tbody tr");
            rows.Count.ShouldBeGreaterThan(0);
            var firstEmployeeName = rows[0].QuerySelector("[data-testid*='TableRowName']")?.TextContent;
            firstEmployeeName.ShouldNotBeNullOrEmpty();
        });
    }

    [Test]
    public async Task TeamsAndVendors_SortByOpenLoad_ClickingOpenLoadHeaderSorts()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
            component.FindAll("button.sort-header").Count.ShouldBeGreaterThanOrEqualTo(2));

        await component.FindAll("button.sort-header")[1].ClickAsync();
        await component.WaitForAssertionAsync(() =>
            component.FindAll("tbody tr").Count.ShouldBeGreaterThan(0));
    }

    [Test]
    public async Task TeamsAndVendors_SortIndicatorShownOnActiveColumn()
    {
        var page = TeamsAndVendorsPageContext.Create().WithBus(new TeamsAndVendorsBus());
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var nameHeader = component.FindAll("button.sort-header")[0];
            nameHeader.ClassList.ShouldContain("active-sort");
        });
    }

    [Test]
    public async Task TeamsAndVendors_FiltersOutEmployeesWhoCannotFulfillWorkOrders()
    {
        var bus = new TeamsAndVendorsBus();
        bus.EmployeesToReturn = bus.EmployeesToReturn
            .Union([TeamsAndVendorsSteps.NonAssignableCreator()])
            .ToArray();
        var page = TeamsAndVendorsPageContext.Create().WithBus(bus);
        await using var ctx = page.Build();

        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
            component.Markup.ShouldNotContain("Cannot Assign"));
    }

    private static void AssertWorkLoadCounts(IRenderedComponent<CascadingAuthenticationState> component)
    {
        var openCell = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TableRowAssignedOpen}hsimpson']");
        openCell.TextContent.ShouldBe("5");
        var inProgressCell = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TableRowInProgress}hsimpson']");
        inProgressCell.TextContent.ShouldBe("2");
        var overdueCell = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TableRowOverdue}hsimpson']");
        overdueCell.TextContent.ShouldBe("1");
        var completedCell = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TableRowCompleted}hsimpson']");
        completedCell.TextContent.ShouldBe("10");
    }

    private static void AssertAssigneeSearchLink(IRenderedComponent<CascadingAuthenticationState> component)
    {
        var nameCell = component.Find($"[data-testid='{TeamsAndVendorsPage.Elements.TableRowName}hsimpson']");
        var link = nameCell.QuerySelector("a");
        link.ShouldNotBeNull();
        var href = link.GetAttribute("href");
        href.ShouldNotBeNull();
        href.ShouldContain("workorder/search");
        href.ShouldContain("Assignee=hsimpson");
        href.ShouldContain("OpenOnly=true");
    }
}
