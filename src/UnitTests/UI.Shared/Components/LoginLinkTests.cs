using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LoginLinkTests
{
    [Test]
    public async Task ShouldRenderTooltipWithExactTitleOnLoginLink()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<LoginLink>();

        var link = component.Find($"[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        link.GetAttribute("title").ShouldBe("Sign in to manage your work orders");
    }

    [Test]
    public async Task Should_RenderSignInIcon_WhenAnonymousLoginLinkShown()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<LoginLink>();

        var link = component.Find($"[data-testid='{nameof(LoginLink.Elements.LoginLink)}']");
        var icon = link.Children[0];
        icon.TagName.ShouldBe("I");
        icon.ClassList.ShouldContain("bi");
        icon.ClassList.ShouldContain("bi-box-arrow-in-right");
        icon.GetAttribute("aria-hidden").ShouldBe("true");
        link.TextContent.Trim().ShouldBe("Login");
        link.GetAttribute("title").ShouldBe("Sign in to manage your work orders");
        link.GetAttribute("data-testid").ShouldBe(nameof(LoginLink.Elements.LoginLink));
    }
}
