using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class NotFoundPageTests
{
    [Test]
    public void Should_RenderHomeLink_WithExpectedTextHrefAndTestId()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<NotFound>();

        var link = component.Find($"[data-testid='{nameof(NotFound.Elements.NotFoundHomeLink)}']");
        link.TextContent.ShouldBe("Return to the home page");
        link.GetAttribute("href").ShouldBe("/");
        link.GetAttribute("data-testid").ShouldBe(nameof(NotFound.Elements.NotFoundHomeLink));
    }

    [Test]
    public void Should_RenderAlertIcon_WithExpectedClassesAriaHiddenAndTestId()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<NotFound>();

        var icon = component.Find($"[data-testid='{nameof(NotFound.Elements.NotFoundIcon)}']");
        icon.GetAttribute("class").ShouldBe("bi bi-exclamation-triangle");
        icon.GetAttribute("aria-hidden").ShouldBe("true");
        icon.GetAttribute("data-testid").ShouldBe(nameof(NotFound.Elements.NotFoundIcon));
    }

    [Test]
    public void Should_RenderPageHeaderAndCardLinks_WithDashboardAndWorkOrders()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());

        var component = ctx.Render<NotFound>();

        component.Find("h1").TextContent.ShouldBe("Not found");
        var card = component.Find("section.not-found-card");
        var dashboard = card.QuerySelector($"[data-testid='{nameof(NotFound.Elements.NotFoundDashboardLink)}']");
        dashboard.ShouldNotBeNull();
        dashboard!.TextContent.ShouldBe("Dashboard");
        dashboard.GetAttribute("href").ShouldBe("/");
        var workOrders = card.QuerySelector($"[data-testid='{nameof(NotFound.Elements.NotFoundWorkOrdersLink)}']");
        workOrders.ShouldNotBeNull();
        workOrders!.TextContent.ShouldBe("Work Orders");
        workOrders.GetAttribute("href").ShouldBe("/workorder/search");
        var home = card.QuerySelector($"[data-testid='{nameof(NotFound.Elements.NotFoundHomeLink)}']");
        home.ShouldNotBeNull();
        home!.GetAttribute("href").ShouldBe("/");
    }
}
