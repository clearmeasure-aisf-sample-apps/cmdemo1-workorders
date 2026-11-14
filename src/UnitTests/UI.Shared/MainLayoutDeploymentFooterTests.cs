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
public class MainLayoutDeploymentFooterTests
{
    [Test]
    public async Task ShouldRenderGitSha_WithCorrectHref_WhenEndpointReturnsValidSha()
    {
        await using var ctx = CreateContext(gitSha: "abc1234def5678901");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

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
        await using var ctx = CreateContext(gitSha: "abc1234def5678901");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

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
        await using var ctx = CreateContext(environmentName: "Staging");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("Staging");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenEndpointThrows()
    {
        await using var ctx = CreateContext(simulateHttpError: true);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForEnvironmentName_WhenEndpointThrows()
    {
        await using var ctx = CreateContext(simulateHttpError: true);

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenResponseOmitsGitShaProperty()
    {
        await using var ctx = CreateContext(rawJsonBody: "{\"version\":\"1.0.0\"}");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForEnvironmentName_WhenResponseOmitsEnvironmentNameProperty()
    {
        await using var ctx = CreateContext(rawJsonBody: "{\"version\":\"1.0.0\"}");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var span = layout.Find($"[data-testid='{nameof(MainLayout.Elements.EnvironmentName)}']");
        span.TextContent.Trim().ShouldBe("unknown");
    }

    [Test]
    public async Task ShouldRenderUnknown_ForGitSha_WhenResponseBodyIsMalformedJson()
    {
        await using var ctx = CreateContext(rawJsonBody: "{not valid json");

        var component = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());
        var layout = component.FindComponent<MainLayout>();

        var element = layout.Find($"[data-testid='{nameof(MainLayout.Elements.GitSha)}']");
        element.TextContent.Trim().ShouldBe("unknown");
    }

    private static BunitContext CreateContext(
        string? gitSha = null,
        string? environmentName = null,
        bool simulateHttpError = false,
        string? rawJsonBody = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.AddAuthorization();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession());
        ctx.Services.AddSingleton<IFeatureFlags>(new StubFeatureFlags());
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        ctx.Services.AddSingleton(new CustomAuthenticationStateProvider(new StubUserSessionStore()));
        var handler = new StubEnvironmentStatusHandler(
            simulateHttpError ? null : new EnvironmentStatusStub(
                Version: "1.0.0",
                GitSha: gitSha ?? "unknown",
                EnvironmentName: environmentName ?? "unknown"),
            rawJsonBody);
        ctx.Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        return ctx;
    }

    private sealed class StubUserSession : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult<Employee?>(null);
    }

    // ReSharper disable NotAccessedPositionalProperty.Local -- properties consumed via JSON serialization reflection
    private sealed record EnvironmentStatusStub(string Version, string GitSha, string EnvironmentName);
    // ReSharper restore NotAccessedPositionalProperty.Local

    private sealed class StubEnvironmentStatusHandler(EnvironmentStatusStub? stub, string? rawJsonBody = null) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (rawJsonBody is not null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(rawJsonBody, System.Text.Encoding.UTF8, "application/json")
                });
            }

            if (stub is null)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var json = JsonSerializer.Serialize(stub, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
