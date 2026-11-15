using System.Globalization;
using Bunit;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Shell;

[TestFixture]
public class AppFooterTests
{
    [Test]
    public async Task ShouldRenderCopyrightFooter_WithCurrentYear_OrganizationAndLink_WhenNotAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.TagName.ShouldBe("FOOTER");
        layout.FindAll("#app-navigation-rail footer").Count.ShouldBe(0);

        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        string.Join(' ', footer.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ShouldContain($"© {yearText} Clear Measure");

        var link = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link");
        link.GetAttribute("href")!.TrimEnd('/').ShouldBe("https://clearmeasure.com");
        var linkText = string.Join(' ', link.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        linkText.ShouldContain("Clear Measure (opens in a new tab)");
    }

    [Test]
    public async Task ShouldRenderCopyrightFooter_WhenAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        string.Join(' ', footer.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ShouldContain($"© {yearText} Clear Measure");
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link").GetAttribute("href")!.TrimEnd('/')
            .ShouldBe("https://clearmeasure.com");
    }

    [Test]
    public async Task ShouldRenderSoftwareVersion_WithinSiteFooter()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var versionSpan = layout.Find($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']");
        versionSpan.TextContent.Trim().ShouldNotBeEmpty();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderSoftwareVersion_WhenUserIsAuthenticated()
    {
        await using var ctx = ShellLayoutContext.Create(authenticateAsUser: "hsimpson");
        var layout = RenderLayout(ctx);

        var versionSpan = layout.Find($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']");
        versionSpan.ShouldNotBeNull();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderFooterNote_WithinSiteFooter()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var note = layout.Find($"[data-testid='{nameof(MainLayout.Elements.FooterNote)}']");
        note.TextContent.Trim().ShouldBe("Facility requests are typically reviewed within one business day.");

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.FooterNote)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderCompanyLink_WithAccessibleAttributes_WhenExternalLinkUsesNewTab()
    {
        await using var ctx = ShellLayoutContext.Create();
        var layout = RenderLayout(ctx);

        var link = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link");
        link.GetAttribute("target").ShouldBe("_blank");
        var rel = link.GetAttribute("rel");
        rel.ShouldNotBeNull();
        rel.ShouldContain("noopener");
        rel.ShouldContain("noreferrer");
        link.TextContent.Trim().ShouldNotContain("://");
        var hidden = link.QuerySelector(".visually-hidden");
        hidden.ShouldNotBeNull();
        hidden.TextContent.ShouldBe("(opens in a new tab)");
        hidden.ClassList.ShouldContain("visually-hidden");
        var linkText = string.Join(' ', link.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        linkText.ShouldContain("Clear Measure (opens in a new tab)");
    }

    [Test]
    public async Task ShouldRenderGitSha_WithCorrectHref_WhenEndpointReturnsValidSha()
    {
        var bus = ShellLayoutContext.BusWithDeployment(new DeploymentIdentity("abc1234def5678901", "unknown"));
        await using var ctx = ShellLayoutContext.Create(bus: bus);
        var layout = RenderLayout(ctx);

        var anchor = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        anchor.TagName.ShouldBe("A");
        var href = anchor.GetAttribute("href");
        href.ShouldNotBeNull();
        href.ShouldContain("github.com/clearmeasure-aisf-sample-apps/cmdemo1-workorders/commit/abc1234def5678901");
        anchor.GetAttribute("target").ShouldBe("_blank");
        var rel = anchor.GetAttribute("rel");
        rel.ShouldNotBeNull();
        rel.ShouldContain("noopener");
        rel.ShouldContain("noreferrer");
    }

    [Test]
    public async Task ShouldTruncateGitSha_ToSevenChars_ForDisplayText()
    {
        var bus = ShellLayoutContext.BusWithDeployment(new DeploymentIdentity("abc1234def5678901", "unknown"));
        await using var ctx = ShellLayoutContext.Create(bus: bus);
        var layout = RenderLayout(ctx);

        var anchor = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        var linkText = string.Join(' ', anchor.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        linkText.ShouldContain("abc1234");
        linkText.ShouldBe("Commit abc1234 (opens in a new tab)");
        var hidden = anchor.QuerySelectorAll(".visually-hidden");
        hidden.Length.ShouldBe(2);
        hidden[0].TextContent.ShouldBe("Commit ");
        hidden[1].TextContent.ShouldBe(" (opens in a new tab)");
    }

    [Test]
    public async Task ShouldRenderEnvironmentName_FromEndpointResponse()
    {
        var bus = ShellLayoutContext.BusWithDeployment(new DeploymentIdentity("unknown", "Staging"));
        await using var ctx = ShellLayoutContext.Create(bus: bus);
        var layout = RenderLayout(ctx);

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("Staging");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenEndpointThrows()
    {
        await using var ctx = ShellLayoutContext.Create(bus: ShellLayoutContext.BusThatFailsDeployment());
        var layout = RenderLayout(ctx);

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForEnvironmentName_WhenEndpointThrows()
    {
        await using var ctx = ShellLayoutContext.Create(bus: ShellLayoutContext.BusThatFailsDeployment());
        var layout = RenderLayout(ctx);

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitShaAndEnvironment_WhenBusThrows()
    {
        await using var ctx = ShellLayoutContext.Create(bus: ShellLayoutContext.BusThatFailsDeployment());
        var layout = RenderLayout(ctx);

        layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']").TextContent.Trim().ShouldBe("unknown");
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']").TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenIdentityIsUnreported()
    {
        var bus = ShellLayoutContext.BusWithDeployment(DeploymentIdentity.Unreported);
        await using var ctx = ShellLayoutContext.Create(bus: bus);
        var layout = RenderLayout(ctx);

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForEnvironmentName_WhenIdentityIsUnreported()
    {
        var bus = ShellLayoutContext.BusWithDeployment(DeploymentIdentity.Unreported);
        await using var ctx = ShellLayoutContext.Create(bus: bus);
        var layout = RenderLayout(ctx);

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("unknown");
    }

    private static IRenderedComponent<CascadingAuthenticationState> RenderLayout(BunitContext ctx) =>
        ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
}
