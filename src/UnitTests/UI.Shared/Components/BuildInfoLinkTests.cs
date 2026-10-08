using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class BuildInfoLinkTests
{
    [Test]
    public async Task Should_RenderBuildInfoLink_WithExpectedAttributes()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());

        var component = ctx.Render<BuildInfoLink>();

        var link = component.Find($"[data-testid='{nameof(BuildInfoLink.Elements.BuildInfoLink)}']");
        link.TextContent.ShouldContain("Build info");
        link.GetAttribute("href").ShouldBe("/_build");
        link.GetAttribute("target").ShouldBe("_blank");
        var rel = link.GetAttribute("rel");
        rel.ShouldNotBeNull();
        rel.ShouldContain("noopener");
        link.GetAttribute("title").ShouldBe("View what this version was built from");
    }
}
