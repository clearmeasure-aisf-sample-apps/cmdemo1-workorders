using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Palermo.BlazorMvc;
using LoginPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.Login;

/// <summary>
/// Fluent bUnit host for <see cref="LoginPage"/> page tests.
/// </summary>
public sealed class LoginPageContext
{
    private IBus? _bus;
    private string _environmentName = "Testing";
    private CustomAuthenticationStateProvider? _auth;

    /// <summary>Starts a page context with default DI wiring.</summary>
    public static LoginPageContext Create() => new();

    /// <summary>Auth provider registered after <see cref="Build"/> (for sign-in assertions).</summary>
    public CustomAuthenticationStateProvider Auth =>
        _auth ?? throw new InvalidOperationException("Call Build() first.");

    /// <summary>Uses a custom <see cref="IBus"/> instead of <see cref="StubBus"/>.</summary>
    public LoginPageContext WithBus(IBus bus)
    {
        _bus = bus;
        return this;
    }

    /// <summary>Overrides <see cref="IHostEnvironment.EnvironmentName"/> (default Testing).</summary>
    public LoginPageContext WithEnvironment(string environmentName)
    {
        _environmentName = environmentName;
        return this;
    }

    /// <summary>Builds a disposable bUnit context with bus, auth, session, and host environment.</summary>
    public BunitContext Build()
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        _auth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        ctx.Services.AddSingleton(_auth);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(_auth);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession { OnSignIn = _auth.Login });
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(_bus ?? new StubBus());
        ctx.Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(_environmentName));
        ctx.Services.AddSingleton(TimeProvider.System);
        return ctx;
    }

    /// <summary>Renders the login page.</summary>
    public IRenderedComponent<LoginPage> Render(BunitContext ctx) => ctx.Render<LoginPage>();

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
