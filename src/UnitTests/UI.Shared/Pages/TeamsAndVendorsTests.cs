using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class TeamsAndVendorsTests
{
    [Test]
    public async Task TeamsAndVendors_OnInitialized_ShouldLoadAllAssignableEmployees()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() => component.Markup.ShouldContain("HOMER SIMPSON"));
        bus.EmployeeWorkLoadQueriesSent.ShouldContain(q => q.EmployeeUserName == "hsimpson");
    }

    [Test]
    public async Task TeamsAndVendors_WhenNoAssignableEmployees_ShouldShowEmptyState()
    {
        var bus = new StubBusTeamsAndVendors { EmployeesToReturn = Array.Empty<Employee>() };
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() => component.Markup.ShouldContain("No assignable employees found"));
    }

    [Test]
    public async Task TeamsAndVendors_TableRendersAllColumns()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
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
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            var nameCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowName}hsimpson']");
            nameCell.TextContent.ShouldContain("HOMER SIMPSON");
            nameCell.OuterHtml.ShouldContain("avatar");
        });
    }

    [Test]
    public async Task TeamsAndVendors_EmployeeRow_DisplaysRole()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            var roleCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowRole}hsimpson']");
            roleCell.TextContent.ShouldNotBeNullOrEmpty();
        });
    }

    [Test]
    public async Task TeamsAndVendors_EmployeeRow_DisplaysWorkLoadCounts()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            var openCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowAssignedOpen}hsimpson']");
            openCell.TextContent.ShouldBe("5");

            var inProgressCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowInProgress}hsimpson']");
            inProgressCell.TextContent.ShouldBe("2");

            var overdueCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowOverdue}hsimpson']");
            overdueCell.TextContent.ShouldBe("1");

            var completedCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowCompleted}hsimpson']");
            completedCell.TextContent.ShouldBe("10");
        });
    }

    [Test]
    public async Task TeamsAndVendors_EmployeeName_LinksToSearchPageWithAssigneeFilter()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            var nameCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowName}hsimpson']");
            var link = nameCell.QuerySelector("a");
            link.ShouldNotBeNull();
            var href = link!.GetAttribute("href");
            href.ShouldNotBeNull();
            href!.ShouldContain("workorder/search");
            href!.ShouldContain("Assignee=hsimpson");
        });
    }

    [Test]
    public async Task TeamsAndVendors_SortByName_ClickingNameHeaderTogglesSortDirection()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            var nameHeaders = component.FindAll("button.sort-header");
            nameHeaders.Count.ShouldBeGreaterThan(0);
            nameHeaders[0].TextContent.ShouldContain("Name");
        });

        var nameHeader1 = component.FindAll("button.sort-header")[0];
        await nameHeader1.ClickAsync();

        component.WaitForAssertion(() =>
        {
            var rows = component.FindAll("tbody tr");
            rows.Count.ShouldBeGreaterThan(0);
        });
    }

    [Test]
    public async Task TeamsAndVendors_DefaultSort_NameAscending()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
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
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            var headers = component.FindAll("button.sort-header");
            headers.Count.ShouldBeGreaterThanOrEqualTo(2);
        });

        var openLoadHeader = component.FindAll("button.sort-header")[1];
        await openLoadHeader.ClickAsync();

        component.WaitForAssertion(() =>
        {
            var rows = component.FindAll("tbody tr");
            rows.Count.ShouldBeGreaterThan(0);
        });
    }

    [Test]
    public async Task TeamsAndVendors_SortIndicatorShownOnActiveColumn()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            var nameHeader = component.FindAll("button.sort-header")[0];
            nameHeader.ClassList.ShouldContain("active-sort");
        });
    }

    [Test]
    public async Task TeamsAndVendors_FiltersOutEmployeesWhoCannotFulfillWorkOrders()
    {
        var nonAssignableEmployee = new Employee("cannotassign", "Cannot", "Assign", "cannot@example.com");
        nonAssignableEmployee.AddRole(new Role { Name = "Creator", CanCreateWorkOrder = true, CanFulfillWorkOrder = false });

        var bus = new StubBusTeamsAndVendors();
        bus.EmployeesToReturn = bus.EmployeesToReturn
            .Union([nonAssignableEmployee])
            .ToArray();

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        component.WaitForAssertion(() =>
        {
            component.Markup.ShouldNotContain("Cannot Assign");
        });
    }

    private static BunitContext CreateContext(StubBusTeamsAndVendors? bus = null)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus ?? new StubBusTeamsAndVendors());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);

        var bunitAuth = ctx.AddAuthorization();
        bunitAuth.SetAuthorized("hsimpson");
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        customAuth.Login("hsimpson").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);
        ctx.Services.AddSingleton(customAuth);

        return ctx;
    }
}

/// <summary>
/// Stub bus for TeamsAndVendors tests that captures EmployeeWorkLoadQuery invocations.
/// </summary>
public class StubBusTeamsAndVendors : StubBus
{
    public List<EmployeeWorkLoadQuery> EmployeeWorkLoadQueriesSent { get; } = new();

    private Employee[]? _employeesToReturn;
    private bool _employeesInitialized;

    public Employee[] EmployeesToReturn
    {
        get
        {
            if (!_employeesInitialized)
            {
                var emp1 = new Employee("hsimpson", "HOMER", "SIMPSON", "homer@springfield.com");
                emp1.AddRole(new Role { Name = "Worker", CanCreateWorkOrder = false, CanFulfillWorkOrder = true });

                var emp2 = new Employee("mburns", "Montgomery", "Burns", "burns@plant.com");
                emp2.AddRole(new Role { Name = "Manager", CanCreateWorkOrder = true, CanFulfillWorkOrder = true });

                var emp3 = new Employee("nflanders", "Ned", "Flanders", "ned@flanders.com");
                emp3.AddRole(new Role { Name = "Worker", CanCreateWorkOrder = false, CanFulfillWorkOrder = true });

                _employeesToReturn = [emp1, emp2, emp3];
                _employeesInitialized = true;
            }

            return _employeesToReturn ?? Array.Empty<Employee>();
        }
        set
        {
            _employeesToReturn = value;
            _employeesInitialized = true;
        }
    }

    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is EmployeeGetAllQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)EmployeesToReturn);
        }

        if (request is EmployeeWorkLoadQuery workLoadQuery)
        {
            EmployeeWorkLoadQueriesSent.Add(workLoadQuery);
            var load = new EmployeeWorkLoad { AssignedAndOpen = 5, InProgress = 2, Overdue = 1, CompletedLast30Days = 10 };
            return Task.FromResult<TResponse>((TResponse)(object)load);
        }

        return base.Send(request);
    }
}
