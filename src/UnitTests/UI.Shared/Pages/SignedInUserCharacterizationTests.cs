using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

/// <summary>
/// Characterization: pages learn the signed-in user only through <see cref="IUserSession"/>.
/// </summary>
[TestFixture]
public class SignedInUserCharacterizationTests
{
    [Test]
    public async Task Index_ShouldShowNewRequest_WhenSessionEmployeeCanCreate()
    {
        var user = new Employee("tlovejoy", "Timothy", "Lovejoy", "tl@example.com");
        user.AddRole(new Role("r", true, false));
        await using var ctx = CreateIndexContext(user);

        var component = ctx.Render<IndexPage>();

        component.FindAll($"[data-testid='{IndexPage.Elements.QuickLinkNewRequest}']").Count.ShouldBe(1);
    }

    [Test]
    public async Task Index_ShouldHideDashboard_WhenSessionHasNoUserName()
    {
        await using var ctx = CreateIndexContext(null);

        var component = ctx.Render<IndexPage>();

        component.Find($"[data-testid='{IndexPage.Elements.SignInPrompt}']").TextContent
            .ShouldBe("Sign in to see your work orders.");
    }

    [Test]
    public async Task Logout_ShouldShowSessionUserName_WhenEmployeeLookupFails()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(userName: "hsimpson")
        {
            GetCurrentUserFault = new InvalidOperationException("lookup failed")
        });

        var component = ctx.Render<Logout>();

        Normalize(component.Find($"[data-testid='{nameof(Logout.Elements.WelcomeText)}']").TextContent)
            .ShouldBe("Welcome hsimpson!");
    }

    [Test]
    public async Task Logout_ShouldNavigateAndNotify_WhenSignedOutThroughSession()
    {
        await using var ctx = new BunitContext();
        var spy = new SpyUiBus();
        var signedOut = false;
        ctx.Services.AddSingleton<IUiBus>(spy);
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(userName: "hsimpson")
        {
            OnSignOut = () =>
            {
                signedOut = true;
                return Task.CompletedTask;
            }
        });

        var component = ctx.Render<Logout>();
        await component.Find($"[data-testid='{nameof(Logout.Elements.LogoutLink)}']").ClickAsync(new());

        signedOut.ShouldBeTrue();
        spy.NotifyWasCalled.ShouldBeTrue();
        spy.LastNotifiedEvent.ShouldBeOfType<UserLoggedOutEvent>();
        ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri
            .ShouldEndWith("/login");
    }

    private static BunitContext CreateIndexContext(Employee? user)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(user));
        var auth = ctx.AddAuthorization();
        if (user != null)
        {
            auth.SetAuthorized(user.UserName);
        }

        return ctx;
    }

    private static string Normalize(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
}
