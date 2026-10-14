using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class NavMenuTests
{
    [Test]
    public async Task ShouldRenderWorkspaceQueueAndAdministrationSections()
    {
        await using var ctx = CreateContext(out _);

        var component = ctx.Render<NavMenu>();

        component.FindAll("h2.nav-section").Select(h => h.TextContent.Trim())
            .ShouldBe(["Workspace", "My Queue", "Administration"]);
        foreach (var heading in component.FindAll("h2.nav-section"))
        {
            component.Find($"ul[aria-labelledby='{heading.Id}']").ShouldNotBeNull();
        }

        var text = component.Markup;
        foreach (var label in new[] { "Dashboard", "Work Orders", "New Request", "AI Assistant", "Assigned to Me", "Created by Me", "In Progress", "Overdue", "Buildings &amp; Rooms", "Teams &amp; Vendors", "Reports", "Settings" })
        {
            text.ShouldContain(label);
        }
    }

    [Test]
    public async Task ShouldRenderQueueBadgesFromSummary()
    {
        await using var ctx = CreateContext(out var bus);
        bus.Summary = new WorkOrderSearchSummary { OpenCount = 24, AssignedToMeTotal = 2, InProgressCount = 5, OverdueCount = 3 };

        var component = ctx.Render<NavMenu>();

        BadgeText(component, NavMenu.Elements.Search).ShouldBe("24");
        BadgeText(component, NavMenu.Elements.WorkOrdersAssignedToMe).ShouldBe("2");
        BadgeText(component, NavMenu.Elements.AllWorkOrdersInProgress).ShouldBe("5");
        BadgeText(component, NavMenu.Elements.Overdue).ShouldBe("3");
        Link(component, NavMenu.Elements.Overdue).GetAttribute("href").ShouldBe("workorder/search?OverdueOnly=true");
    }

    [Test]
    public async Task ShouldUseDangerAccentForOverdue()
    {
        await using var ctx = CreateContext(out _);

        var component = ctx.Render<NavMenu>();

        Link(component, NavMenu.Elements.Overdue).ClassList.ShouldContain("nav-link--danger");
        Link(component, NavMenu.Elements.WorkOrdersAssignedToMe).ClassList.ShouldNotContain("nav-link--danger");
    }

    [Test]
    public async Task ShouldRenderBuildingsAndRoomsLink_AsEnabledNavLink()
    {
        await using var ctx = CreateContext(out _);

        var component = ctx.Render<NavMenu>();

        var buildingsLink = component.Find($"[data-testid='{NavMenu.Elements.BuildingsAndRooms}']");
        buildingsLink.TagName.ShouldBe("A");
        buildingsLink.ClassList.ShouldContain("nav-link");
        buildingsLink.GetAttribute("href").ShouldBe("admin/buildings");
        buildingsLink.TextContent.ShouldContain("Buildings & Rooms");
        buildingsLink.TextContent.ShouldNotContain("Soon");
    }

    [Test]
    public async Task ShouldRenderReportsLink_AsEnabledNavLink()
    {
        await using var ctx = CreateContext(out _);

        var component = ctx.Render<NavMenu>();

        var reportsLink = component.Find($"[data-testid='{NavMenu.Elements.Reports}']");
        reportsLink.TagName.ShouldBe("A");
        reportsLink.ClassList.ShouldContain("nav-link");
        reportsLink.GetAttribute("href").ShouldBe("reports");
        reportsLink.TextContent.ShouldContain("Reports");
        reportsLink.TextContent.ShouldNotContain("Soon");
    }

    [Test]
    public async Task ShouldNotRenderMenuLabel()
    {
        await using var ctx = CreateContext(out _);

        var component = ctx.Render<NavMenu>();

        component.FindAll("h2.nav-section").ShouldNotContain(h => h.TextContent.Trim() == "Menu");
        component.Markup.ShouldNotContain(">Menu<");
        component.FindAll("[data-testid='AllAssignedWorkOrders']").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldHideNewRequest_WhenUserCannotCreateWorkOrder()
    {
        var user = new Employee("fixer", "Groundskeeper", "Willie", "w@example.com");
        user.AddRole(new Role("fulfiller", false, true));
        await using var ctx = CreateContext(out _, user);

        var component = ctx.Render<NavMenu>();

        component.FindAll($"[data-testid='{NavMenu.Elements.NewWorkOrder}']").Count.ShouldBe(0);
        component.FindAll($"[data-testid='{NavMenu.Elements.Search}']").Count.ShouldBe(1);
    }

    [Test]
    public async Task ShouldRefreshBadges_WhenWorkOrderChanged()
    {
        await using var ctx = CreateContext(out var bus);
        bus.Summary = new WorkOrderSearchSummary { OpenCount = 1 };
        var component = ctx.Render<NavMenu>();
        BadgeText(component, NavMenu.Elements.Search).ShouldBe("1");

        bus.Summary = new WorkOrderSearchSummary { OpenCount = 2 };
        component.Instance.Handle(new WorkOrderChangedEvent(new StateCommandResult(new WorkOrder { Number = "X" }, "Save", "saved")));

        await component.WaitForAssertionAsync(() => BadgeText(component, NavMenu.Elements.Search).ShouldBe("2"));
        bus.SummaryRequestCount.ShouldBe(2);
    }

    [Test]
    public async Task ShouldMarkOnlyMatchingQueueLinkActive()
    {
        await using var ctx = CreateContext(out _);
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("workorder/search?Status=InProgress");

        var component = ctx.Render<NavMenu>();

        Link(component, NavMenu.Elements.AllWorkOrdersInProgress).ClassList.ShouldContain("active");
        Link(component, NavMenu.Elements.AllWorkOrdersInProgress).GetAttribute("aria-current").ShouldBe("page");
        Link(component, NavMenu.Elements.Overdue).ClassList.ShouldNotContain("active");
        Link(component, NavMenu.Elements.Overdue).HasAttribute("aria-current").ShouldBeFalse();

        navigation.NavigateTo("workorder/search?OverdueOnly=true");

        await component.WaitForAssertionAsync(() => Link(component, NavMenu.Elements.Overdue).ClassList.ShouldContain("active"));
        Link(component, NavMenu.Elements.AllWorkOrdersInProgress).ClassList.ShouldNotContain("active");
    }

    private static AngleSharp.Dom.IElement Link(IRenderedComponent<NavMenu> component, NavMenu.Elements testId) =>
        component.Find($"[data-testid='{testId}']");

    private static string BadgeText(IRenderedComponent<NavMenu> component, NavMenu.Elements testId) =>
        Link(component, testId).QuerySelector(".nav-badge")!.FirstChild!.TextContent.Trim();

    private static BunitContext CreateContext(out StubBus bus, Employee? user = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        if (user == null)
        {
            user = new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "t@example.com");
            user.AddRole(new Role("Pastor", true, false));
        }

        bus = new StubBus();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        return ctx;
    }
}
