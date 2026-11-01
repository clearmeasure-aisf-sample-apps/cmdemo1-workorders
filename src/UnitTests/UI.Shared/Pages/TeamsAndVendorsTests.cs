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

        await component.WaitForAssertionAsync(() => component.Markup.ShouldContain("HOMER SIMPSON"));
        bus.EmployeeWorkLoadQueriesSent.ShouldContain(q => q.EmployeeUserName == "hsimpson");
    }

    [Test]
    public async Task TeamsAndVendors_WhenNoAssignableEmployees_ShouldShowEmptyState()
    {
        var bus = new StubBusTeamsAndVendors { EmployeesToReturn = Array.Empty<Employee>() };
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() => component.Markup.ShouldContain("No assignable employees found"));
    }

    [Test]
    public async Task TeamsAndVendors_TableRendersAllColumns()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

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
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
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

        await component.WaitForAssertionAsync(() =>
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

        await component.WaitForAssertionAsync(() =>
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

        await component.WaitForAssertionAsync(() =>
        {
            var nameCell = component.Find($"[data-testid='{TeamsAndVendors.Elements.TableRowName}hsimpson']");
            var link = nameCell.QuerySelector("a");
            link.ShouldNotBeNull();
            var href = link.GetAttribute("href");
            href.ShouldNotBeNull();
            href.ShouldContain("workorder/search");
            href.ShouldContain("Assignee=hsimpson");
            href.ShouldContain("OpenOnly=true");
        });
    }

    [Test]
    public async Task TeamsAndVendors_SortByName_ClickingNameHeaderTogglesSortDirection()
    {
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var nameHeaders = component.FindAll("button.sort-header");
            nameHeaders.Count.ShouldBeGreaterThan(0);
            nameHeaders[0].TextContent.ShouldContain("Name");
        });

        var nameHeader1 = component.FindAll("button.sort-header")[0];
        await nameHeader1.ClickAsync();

        await component.WaitForAssertionAsync(() =>
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
        var bus = new StubBusTeamsAndVendors();
        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var headers = component.FindAll("button.sort-header");
            headers.Count.ShouldBeGreaterThanOrEqualTo(2);
        });

        var openLoadHeader = component.FindAll("button.sort-header")[1];
        await openLoadHeader.ClickAsync();

        await component.WaitForAssertionAsync(() =>
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

        await component.WaitForAssertionAsync(() =>
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

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldNotContain("Cannot Assign");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamsSection_RenderWhenTeamsExist()
    {
        var emp1 = new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com");
        emp1.AddRole(new Role { Name = "Worker", CanCreateWorkOrder = false, CanFulfillWorkOrder = true });
        var team = new Team("Maintenance", "Maintenance team") { Id = Guid.NewGuid() };
        team.AddMember(emp1);

        var bus = new StubBusTeamsAndVendors { TeamsToReturn = [team] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldContain("Maintenance");
            component.Markup.ShouldContain("Maintenance team");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamsSection_ShowsEmptyStateWhenNoTeams()
    {
        var bus = new StubBusTeamsAndVendors { TeamsToReturn = [] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldContain("No teams found");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamCard_DisplaysTeamNameDescription()
    {
        var team = new Team("Kitchen", "Kitchen operations") { Id = Guid.NewGuid() };
        var bus = new StubBusTeamsAndVendors { TeamsToReturn = [team] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var teamCard = component.Find($"[data-testid='{TeamsAndVendors.Elements.TeamCard}{team.Id}']");
            teamCard.TextContent.ShouldContain("Kitchen");
            teamCard.TextContent.ShouldContain("Kitchen operations");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamCard_DisplaysMemberCount()
    {
        var emp1 = new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com");
        emp1.AddRole(new Role { Name = "Worker", CanCreateWorkOrder = false, CanFulfillWorkOrder = true });
        var team = new Team("Test Team", null) { Id = Guid.NewGuid() };
        team.AddMember(emp1);

        var bus = new StubBusTeamsAndVendors { TeamsToReturn = [team] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var memberCountElement = component.Find($"[data-testid='{TeamsAndVendors.Elements.TeamMemberCount}{team.Id}']");
            memberCountElement.TextContent.ShouldContain("1 member");
        });
    }

    [Test]
    public async Task TeamsAndVendors_TeamMembers_LinksToAssigneeFilteredSearch()
    {
        var emp1 = new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com");
        emp1.AddRole(new Role { Name = "Worker", CanCreateWorkOrder = false, CanFulfillWorkOrder = true });
        var team = new Team("Test Team", null) { Id = Guid.NewGuid() };
        team.AddMember(emp1);

        var bus = new StubBusTeamsAndVendors { TeamsToReturn = [team] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var memberLink = component.Find($"[data-testid='{TeamsAndVendors.Elements.TeamMemberLink}{emp1.UserName}']");
            memberLink.ShouldNotBeNull();
            var href = memberLink.GetAttribute("href");
            href.ShouldNotBeNullOrEmpty();
            href.ShouldContain("workorder/search");
            href.ShouldContain("Assignee=hsimpson");
            href.ShouldContain("OpenOnly=true");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorsSection_RenderWhenVendorsExist()
    {
        var vendor = new Vendor { Id = Guid.NewGuid(), Name = "Joe's HVAC", IsActive = true };
        var bus = new StubBusTeamsAndVendors { VendorsToReturn = [vendor] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldContain("Joe's HVAC");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorsSection_ShowsEmptyStateWhenNoVendors()
    {
        var bus = new StubBusTeamsAndVendors { VendorsToReturn = [] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            component.Markup.ShouldContain("No vendors found");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorCard_DisplaysActiveVendorWithStatus()
    {
        var vendor = new Vendor { Id = Guid.NewGuid(), Name = "Active Vendor", IsActive = true };
        var bus = new StubBusTeamsAndVendors { VendorsToReturn = [vendor] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var vendorCard = component.Find($"[data-testid='{TeamsAndVendors.Elements.VendorCard}{vendor.Id}']");
            vendorCard.TextContent.ShouldContain("Active Vendor");
            vendorCard.TextContent.ShouldContain("Active");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorCard_DisplaysInactiveVendorWithStatus()
    {
        var vendor = new Vendor { Id = Guid.NewGuid(), Name = "Inactive Vendor", IsActive = false };
        var bus = new StubBusTeamsAndVendors { VendorsToReturn = [vendor] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var vendorCard = component.Find($"[data-testid='{TeamsAndVendors.Elements.VendorCard}{vendor.Id}']");
            vendorCard.TextContent.ShouldContain("Inactive Vendor");
            vendorCard.TextContent.ShouldContain("Inactive");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorCard_DisplaysAllDetails()
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Joe's HVAC",
            Trade = "HVAC",
            ContactName = "Joe Smith",
            EmailAddress = "joe@hvac.com",
            PhoneNumber = "555-1234",
            IsActive = true
        };
        var bus = new StubBusTeamsAndVendors { VendorsToReturn = [vendor] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var vendorCard = component.Find($"[data-testid='{TeamsAndVendors.Elements.VendorCard}{vendor.Id}']");
            vendorCard.TextContent.ShouldContain("Joe's HVAC");
            vendorCard.TextContent.ShouldContain("HVAC");
            vendorCard.TextContent.ShouldContain("Joe Smith");
            vendorCard.TextContent.ShouldContain("joe@hvac.com");
            vendorCard.TextContent.ShouldContain("555-1234");
        });
    }

    [Test]
    public async Task TeamsAndVendors_VendorEmail_IsMailtoLink()
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = "Test Vendor",
            EmailAddress = "test@vendor.com",
            IsActive = true
        };
        var bus = new StubBusTeamsAndVendors { VendorsToReturn = [vendor] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var emailLink = component.Find($"[data-testid='{TeamsAndVendors.Elements.VendorEmail}{vendor.Id}'] a");
            emailLink.ShouldNotBeNull();
            emailLink.GetAttribute("href").ShouldBe("mailto:test@vendor.com");
        });
    }

    [Test]
    public async Task TeamsAndVendors_Vendors_InactiveAfterActive()
    {
        var activeVendor = new Vendor { Id = Guid.NewGuid(), Name = "Active Vendor", IsActive = true };
        var inactiveVendor = new Vendor { Id = Guid.NewGuid(), Name = "Inactive Vendor", IsActive = false };
        var bus = new StubBusTeamsAndVendors { VendorsToReturn = [activeVendor, inactiveVendor] };

        await using var ctx = CreateContext(bus);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<TeamsAndVendors>());

        await component.WaitForAssertionAsync(() =>
        {
            var vendorCards = component.FindAll("[class*='vendor-card']");
            vendorCards.Count.ShouldBe(2);
            var firstCardName = vendorCards[0].QuerySelector("[class*='vendor-name']")?.TextContent.Trim();
            firstCardName.ShouldBe("Active Vendor");
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

                field = [emp1, emp2, emp3];
                _employeesInitialized = true;
            }

            return field;
        }
        set
        {
            field = value;
            _employeesInitialized = true;
        }
    } = [];

    public Team[] TeamsToReturn { get; init; } = [];

    public Vendor[] VendorsToReturn { get; init; } = [];

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

        if (request is TeamsQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)TeamsToReturn);
        }

        if (request is VendorsQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)VendorsToReturn);
        }

        return base.Send(request);
    }
}
