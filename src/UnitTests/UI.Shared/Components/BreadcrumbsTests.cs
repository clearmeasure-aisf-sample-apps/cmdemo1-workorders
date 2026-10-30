using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class BreadcrumbsTests
{
    [Test]
    public async Task ShouldRenderWorkOrdersSearch_ForSearchRoute()
    {
        await using var ctx = CreateContext("workorder/search?Creator=tlovejoy");

        var component = ctx.Render<Breadcrumbs>();

        Crumbs(component).ShouldBe(["Work Orders", "Search"]);
        component.Find("a").GetAttribute("href").ShouldBe("workorder/search");
    }

    [Test]
    public async Task ShouldRenderWorkOrdersNew_ForManageNew()
    {
        await using var ctx = CreateContext("workorder/manage?mode=New");

        var component = ctx.Render<Breadcrumbs>();

        Crumbs(component).ShouldBe(["Work Orders", "New"]);
    }

    [Test]
    public async Task ShouldRenderDashboard_ForRoot()
    {
        await using var ctx = CreateContext("");

        var component = ctx.Render<Breadcrumbs>();

        Crumbs(component).ShouldBe(["Dashboard"]);
    }

    [Test]
    public async Task ShouldMarkLastCrumbAriaCurrent()
    {
        await using var ctx = CreateContext("workorder/search");

        var component = ctx.Render<Breadcrumbs>();

        component.Find("nav").GetAttribute("aria-label").ShouldBe("Breadcrumb");
        component.FindAll("[aria-current='page']").Single().TextContent.Trim().ShouldBe("Search");
    }

    [Test]
    public async Task ShouldUpdate_WhenLocationChanges()
    {
        await using var ctx = CreateContext("");
        var component = ctx.Render<Breadcrumbs>();

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("settings");

        await component.WaitForAssertionAsync(() => Crumbs(component).ShouldBe(["Settings"]));
    }

    private static string[] Crumbs(IRenderedComponent<Breadcrumbs> component) =>
        component.FindAll("li").Select(li => li.QuerySelector("a, span")!.TextContent.Trim()).ToArray();

    private static BunitContext CreateContext(string relativeUri)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo(relativeUri);
        return ctx;
    }
}
