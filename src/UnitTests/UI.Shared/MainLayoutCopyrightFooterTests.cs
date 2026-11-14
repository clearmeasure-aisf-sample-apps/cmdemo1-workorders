using System.Globalization;
using System.Net;
using System.Text.Json;
using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class MainLayoutCopyrightFooterTests
{
    [Test]
    public async Task ShouldRenderCopyrightFooter_WithCurrentYear_OrganizationAndLink_WhenNotAuthenticated()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

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
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        var yearText = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
        string.Join(' ', footer.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ShouldContain($"© {yearText} Clear Measure");
        layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}'] .site-footer-link").GetAttribute("href")!.TrimEnd('/').ShouldBe("https://clearmeasure.com");
    }

    [Test]
    public async Task ShouldRenderSoftwareVersion_WithinSiteFooter()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var versionSpan = layout.Find($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']");
        versionSpan.TextContent.Trim().ShouldNotBeEmpty();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderSoftwareVersion_WhenUserIsAuthenticated()
    {
        await using var ctx = CreateContext(authenticateAsUser: "hsimpson");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var versionSpan = layout.Find($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']");
        versionSpan.ShouldNotBeNull();

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.SoftwareVersion)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderFooterNote_WithinSiteFooter()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var note = layout.Find($"[data-testid='{nameof(MainLayout.Elements.FooterNote)}']");
        note.TextContent.Trim().ShouldBe("Facility requests are typically reviewed within one business day.");

        var footer = layout.Find($"[data-testid='{nameof(MainLayout.Elements.CopyrightFooter)}']");
        footer.QuerySelector($"[data-testid='{nameof(MainLayout.Elements.FooterNote)}']").ShouldNotBeNull();
    }

    [Test]
    public async Task ShouldRenderCompanyLink_WithAccessibleAttributes_WhenExternalLinkUsesNewTab()
    {
        await using var ctx = CreateContext();

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

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

    private static BunitContext CreateContext(string? authenticateAsUser = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bunitAuth = ctx.AddAuthorization();
        if (authenticateAsUser != null)
            bunitAuth.SetAuthorized(authenticateAsUser);

        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IFeatureFlags>(new StubFeatureFlags());
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        if (authenticateAsUser != null)
            customAuth.Login(authenticateAsUser).GetAwaiter().GetResult();

        ctx.Services.AddSingleton(customAuth);
        var stub = new EnvironmentStatusStub("1.0.0", "unknown", "unknown");
        ctx.Services.AddSingleton(new HttpClient(new StubEnvironmentStatusHandler(stub))
        {
            BaseAddress = new Uri("http://localhost/")
        });
        return ctx;
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(null);
    }

    // ReSharper disable NotAccessedPositionalProperty.Local -- properties consumed via JSON serialization reflection
    private sealed record EnvironmentStatusStub(string Version, string GitSha, string EnvironmentName);
    // ReSharper restore NotAccessedPositionalProperty.Local

    private sealed class StubEnvironmentStatusHandler(EnvironmentStatusStub stub) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(stub, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
