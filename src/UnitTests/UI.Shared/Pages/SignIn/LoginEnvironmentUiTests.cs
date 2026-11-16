using Bunit;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.SignIn;

[TestFixture]
public class LoginEnvironmentUiTests
{
    [Test]
    public async Task Should_ShowVersionLabel_WithVersionPrefix()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var versionLabel = component.FindAll("small.text-muted")
            .First(s => s.TextContent.StartsWith("Version "));

        versionLabel.TextContent.ShouldStartWith("Version ");
        versionLabel.TextContent.ShouldContain(" · Environment ");
    }

    [Test]
    public async Task Should_ShowVersionLabel_WithNonEmptyVersionString()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var versionLabel = component.FindAll("small.text-muted")
            .First(s => s.TextContent.StartsWith("Version "));
        var versionString = versionLabel.TextContent["Version ".Length..];
        var envSegment = versionLabel.TextContent.Split(" · Environment ", 2);

        versionString.ShouldNotBeNullOrWhiteSpace();
        envSegment.Length.ShouldBe(2);
        envSegment[1].ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_ShowVersionLabel_WithEnvironmentSegment()
    {
        var page = LoginPageContext.Create();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var versionLabel = component.FindAll("small.text-muted")
            .First(s => s.TextContent.StartsWith("Version "));

        versionLabel.TextContent.ShouldContain("Environment Testing");
    }

    [Test]
    public async Task Should_ShowVersionLabel_WithFallback_WhenEnvironmentNameIsBlank()
    {
        var page = LoginPageContext.Create().WithEnvironment("");
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        var versionLabel = component.FindAll("small.text-muted")
            .First(s => s.TextContent.StartsWith("Version "));

        versionLabel.TextContent.ShouldContain("Environment unknown");
    }
}
