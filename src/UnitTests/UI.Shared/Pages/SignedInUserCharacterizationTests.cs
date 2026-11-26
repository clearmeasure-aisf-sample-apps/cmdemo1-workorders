using Bunit;
using Bunit.TestDoubles;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Palermo.BlazorMvc;
using Shouldly;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;
using WorkOrderSearchPage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch;

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
        var user = Creator("tlovejoy");
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
        ctx.Services.AddLogging();
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
        ctx.Services.AddLogging();
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
        ctx.Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/login");
    }

    [Test]
    public async Task WorkOrderSearch_ShouldUseSessionUser_ForViewsAndCreateButton()
    {
        var user = Creator("jpalermo");
        var session = new StubUserSession(user);
        await using var ctx = CreateSearchContext(session);

        var component = ctx.Render<WorkOrderSearchPage>();

        component.FindComponent<SearchSavedViewsMenu>().Instance.CurrentUserName.ShouldBe("jpalermo");
        component.FindAll($"[data-testid='{WorkOrderSearchPage.Elements.NewWorkOrderButton}']").Count.ShouldBe(1);
        session.GetCurrentUserNameCallCount.ShouldBeGreaterThan(0);
        session.GetCurrentUserCallCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task MainLayout_ShouldLoadNotifications_ForSessionUser()
    {
        var orders = new WorkOrder[] { new() { Number = "WO-1" } };
        var panel = new NotificationsSummary(orders, [], []);
        var bus = new NotificationsStubBus(panel);
        var session = new StubUserSession(userName: "hsimpson");
        await using var ctx = CreateLayoutContext(session, bus);

        var layout = ctx.Render<CascadingAuthenticationState>(p => p.AddChildContent<MainLayout>());

        await layout.WaitForAssertionAsync(() =>
            layout.Find($"[data-testid='{nameof(MainLayout.Elements.NotificationsBadge)}']")
                .TextContent.Trim().ShouldBe("1"));
        bus.LastUserName.ShouldBe("hsimpson");
        session.GetCurrentUserNameCallCount.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Login_ShouldSignInThroughUserSession()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddLogging();
        var auth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var session = new StubUserSession { OnSignIn = auth.Login };
        ctx.Services.AddSingleton(auth);
        ctx.Services.AddSingleton<AuthenticationStateProvider>(auth);
        ctx.Services.AddSingleton<IUserSession>(session);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IBus>(new StubBus());
        ctx.Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
        ctx.Services.AddSingleton(TimeProvider.System);

        var component = ctx.Render<Login>();
        await component.Find($"[data-testid='{Login.Elements.User}']").ChangeAsync(new() { Value = "hsimpson" });
        await component.Find($"[data-testid='{Login.Elements.LoginButton}']").ClickAsync(new());

        session.SignInUserNames.ShouldBe(["hsimpson"]);
        auth.GetUsername().ShouldBe("hsimpson");
    }

    [Test]
    public async Task ApplicationChat_ShouldSendSessionUserName_OnPrompt()
    {
        var bus = new CapturingChatBus();
        await using var ctx = CreateChatContext(new StubUserSession(userName: "hsimpson"), bus);

        var component = ctx.Render<ApplicationChat>();
        await component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']")
            .ChangeAsync(new() { Value = "hello" });
        await component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() => bus.LastUserName.ShouldBe("hsimpson"));
    }

    [Test]
    public async Task ApplicationChat_ShouldSendAnonymous_WhenSessionHasNoUser()
    {
        var bus = new CapturingChatBus();
        await using var ctx = CreateChatContext(new StubUserSession(), bus);

        var component = ctx.Render<ApplicationChat>();
        await component.Find($"[data-testid='{ApplicationChat.Elements.ChatInput}']")
            .ChangeAsync(new() { Value = "hello" });
        await component.Find($"[data-testid='{ApplicationChat.Elements.SendButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() => bus.LastUserName.ShouldBe("anonymous"));
    }

    private static Employee Creator(string userName)
    {
        var user = new Employee(userName, "First", "Last", $"{userName}@example.com");
        user.AddRole(new Role("creator", true, false));
        return user;
    }

    private static BunitContext CreateIndexContext(Employee? user)
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
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

    private static BunitContext CreateSearchContext(StubUserSession session)
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var bus = new StubBus();
        var uiBus = new StubUiBus();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUserSession>(session);
        ctx.Services.AddSingleton<IUiBus>(uiBus);
        ctx.Services.AddSingleton(new WorkOrderBulkActionService(bus, uiBus));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<ChurchCalendar>();
        ctx.Services.AddSingleton(new WorkOrderSearchState());
        ctx.Services.AddSingleton<IFeatureFlags>(new StubFeatureFlags());
        return ctx;
    }

    private static BunitContext CreateLayoutContext(StubUserSession session, IBus bus)
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.AddAuthorization().SetAuthorized(session.UserName ?? "anon");
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(bus);
        ctx.Services.AddSingleton<IUserSession>(session);
        ctx.Services.AddSingleton<IFeatureFlags>(new StubFeatureFlags());
        ctx.Services.AddSingleton(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton<ThemePreferenceService>();
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<ChurchCalendar>();
        var customAuth = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        if (session.UserName != null)
        {
            customAuth.Login(session.UserName).GetAwaiter().GetResult();
        }

        ctx.Services.AddSingleton(customAuth);
        return ctx;
    }

    private static BunitContext CreateChatContext(StubUserSession session, IBus bus)
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.JSInterop.SetupVoid("scrollToBottom", _ => true).SetVoidResult();
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton(bus);
        ctx.Services.AddSingleton(new CustomAuthenticationStateProvider(new StubUserSessionStore()));
        ctx.Services.AddSingleton<IUserSession>(session);
        return ctx;
    }

    private static string Normalize(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

    private sealed class NotificationsStubBus(NotificationsSummary? result) : StubBus
    {
        public string? LastUserName { get; private set; }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is not NotificationsPanelQuery query)
            {
                return base.Send(request);
            }

            LastUserName = query.UserName;
            return Task.FromResult((TResponse)(object)result!);
        }
    }

    private sealed class CapturingChatBus() : Bus(null!)
    {
        public string? LastUserName { get; private set; }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ApplicationChatQuery chat)
            {
                LastUserName = chat.CurrentUsername;
                return Task.FromResult((TResponse)(object)new ApplicationChatResult("ok"));
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

}
