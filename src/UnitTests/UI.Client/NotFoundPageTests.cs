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
        link.TextContent.ShouldBe("Back to the home page");
        link.GetAttribute("href").ShouldBe("/");
        link.GetAttribute("data-testid").ShouldBe(nameof(NotFound.Elements.NotFoundHomeLink));
    }
}
