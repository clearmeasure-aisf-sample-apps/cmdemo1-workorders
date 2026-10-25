using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class PageHeaderTests
{
    [Test]
    public async Task ShouldRenderTitleTooltipAndTestIds()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<PageHeader>(p => p
            .Add(c => c.Title, "Dashboard")
            .Add(c => c.TitleTooltip, "Work order home")
            .Add(c => c.TitleTestId, "HeadingId")
            .Add(c => c.Subtitle, "Track work.")
            .Add(c => c.SubtitleTestId, "SubtitleId"));

        var heading = component.Find("header.page-header .page-heading h1");
        heading.TextContent.ShouldBe("Dashboard");
        heading.GetAttribute("title").ShouldBe("Work order home");
        heading.GetAttribute("data-testid").ShouldBe("HeadingId");
        var subtitle = component.Find("p.page-subtitle");
        subtitle.TextContent.ShouldBe("Track work.");
        subtitle.GetAttribute("data-testid").ShouldBe("SubtitleId");
    }

    [Test]
    public async Task ShouldRenderActionsInsidePageActions()
    {
        await using var ctx = new BunitContext();
        RenderFragment actions = builder =>
        {
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "class", "btn-app");
            builder.AddAttribute(2, "data-testid", "ActionLink");
            builder.CloseElement();
        };

        var component = ctx.Render<PageHeader>(p => p
            .Add(c => c.Title, "Dashboard")
            .Add(c => c.Actions, actions));

        component.FindAll(".page-actions [data-testid='ActionLink']").Count.ShouldBe(1);
    }

    [Test]
    public async Task ShouldOmitSubtitle_WhenNull()
    {
        await using var ctx = new BunitContext();

        var component = ctx.Render<PageHeader>(p => p.Add(c => c.Title, "Dashboard"));

        component.FindAll(".page-subtitle").Count.ShouldBe(0);
    }

    [Test]
    public async Task ShouldRenderTitleAddonInsideHeading()
    {
        await using var ctx = new BunitContext();
        RenderFragment addon = builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-testid", "NumberId");
            builder.AddContent(2, "42");
            builder.CloseElement();
        };

        var component = ctx.Render<PageHeader>(p => p
            .Add(c => c.Title, "Work Order #")
            .Add(c => c.TitleAddon, addon));

        var heading = component.Find("header.page-header .page-heading h1");
        heading.TextContent.ShouldBe("Work Order #42");
        component.Find("h1 [data-testid='NumberId']").TextContent.ShouldBe("42");
    }
}
