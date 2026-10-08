using Bunit;
using ClearMeasure.Bootcamp.UI.Client.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class NotFoundPageTests
{
    [Test]
    public void Should_RenderHomeLink_WithExpectedTextHrefAndTestId()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<NotFound>();

        var link = component.Find($"[data-testid='{nameof(NotFound.Elements.NotFoundHomeLink)}']");
        link.TextContent.ShouldBe("Back to the home page");
        link.GetAttribute("href").ShouldBe("/");
        link.GetAttribute("data-testid").ShouldBe(nameof(NotFound.Elements.NotFoundHomeLink));
    }
}
