using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.TeamsAndVendors;

/// <summary>
/// Fluent bUnit host for <see cref="ClearMeasure.Bootcamp.UI.Shared.Pages.TeamsAndVendors"/> page tests.
/// </summary>
public sealed class TeamsAndVendorsPageContext
{
    private TeamsAndVendorsBus? _bus;
    private IFeatureFlags? _featureFlags;

    /// <summary>Starts a page context with default DI wiring.</summary>
    public static TeamsAndVendorsPageContext Create() => new();

    /// <summary>Configurable bus used after <see cref="Build"/>.</summary>
    public TeamsAndVendorsBus Bus =>
        _bus ?? throw new InvalidOperationException("Call Build() first.");

    /// <summary>Uses a custom <see cref="TeamsAndVendorsBus"/> instead of a default instance.</summary>
    public TeamsAndVendorsPageContext WithBus(TeamsAndVendorsBus bus)
    {
        _bus = bus;
        return this;
    }

    /// <summary>Overrides feature flags for the page.</summary>
    public TeamsAndVendorsPageContext WithFeatureFlags(IFeatureFlags featureFlags)
    {
        _featureFlags = featureFlags;
        return this;
    }

    /// <summary>Builds a disposable bUnit context with teams-and-vendors page dependencies.</summary>
    public BunitContext Build()
    {
        _bus ??= new TeamsAndVendorsBus();
        var ctx = new BunitContext();
        RegisterServices(ctx);
        return ctx;
    }

    /// <summary>
    /// Renders <see cref="CascadingAuthenticationState"/> wrapping the teams-and-vendors page.
    /// </summary>
    public IRenderedComponent<CascadingAuthenticationState> Render(BunitContext ctx) =>
        ctx.Render<CascadingAuthenticationState>(p =>
            p.AddChildContent<ClearMeasure.Bootcamp.UI.Shared.Pages.TeamsAndVendors>());

    private void RegisterServices(BunitContext ctx)
    {
        ctx.Services.AddSingleton<IBus>(Bus);
        ctx.Services.AddSingleton(_featureFlags ?? new StubFeatureFlags());
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(TimeProvider.System);
        RegisterAuth(ctx);
    }

    private static void RegisterAuth(BunitContext ctx)
    {
        var bunitAuth = ctx.AddAuthorization();
        bunitAuth.SetAuthorized("hsimpson");
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        customAuth.Login("hsimpson").GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(customAuth);
        ctx.Services.AddSingleton(customAuth);
    }
}
