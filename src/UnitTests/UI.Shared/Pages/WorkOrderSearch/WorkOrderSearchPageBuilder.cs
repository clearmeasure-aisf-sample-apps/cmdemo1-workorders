using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderSearch;

/// <summary>
/// Fluent bUnit host for <see cref="ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderSearch"/> page tests.
/// </summary>
public sealed class WorkOrderSearchPageBuilder
{
    /// <summary>Default signed-in user name for search page tests.</summary>
    public const string DefaultUserName = "jpalermo";

    private IBus? _bus;
    private string _loggedInAs = DefaultUserName;
    private WorkOrderSearchState? _searchState;
    private Employee? _currentUser;
    private StubFeatureFlags? _featureFlags;
    private WorkOrder[]? _orders;

    /// <summary>Starts a page builder with default DI wiring.</summary>
    public static WorkOrderSearchPageBuilder Create() => new();

    /// <summary>Uses a custom <see cref="IBus"/> instead of <see cref="StubBus"/>.</summary>
    public WorkOrderSearchPageBuilder WithBus(IBus bus)
    {
        _bus = bus;
        return this;
    }

    /// <summary>Sets the authenticated user name for the session and auth provider.</summary>
    public WorkOrderSearchPageBuilder SignedInAs(string userName)
    {
        _loggedInAs = userName;
        return this;
    }

    /// <summary>Sets the <see cref="IUserSession"/> current employee.</summary>
    public WorkOrderSearchPageBuilder WithCurrentUser(Employee currentUser)
    {
        _currentUser = currentUser;
        return this;
    }

    /// <summary>Injects a pre-populated <see cref="WorkOrderSearchState"/>.</summary>
    public WorkOrderSearchPageBuilder WithSearchState(WorkOrderSearchState searchState)
    {
        _searchState = searchState;
        return this;
    }

    /// <summary>Overrides feature flags for the page.</summary>
    public WorkOrderSearchPageBuilder WithFeatureFlags(StubFeatureFlags featureFlags)
    {
        _featureFlags = featureFlags;
        return this;
    }

    /// <summary>Feeds search results through a default <see cref="StubBus"/>.</summary>
    public WorkOrderSearchPageBuilder WithOrders(params WorkOrder[] orders)
    {
        _orders = orders;
        return this;
    }

    /// <summary>Builds a disposable bUnit context with logging, JS interop, bus, and auth.</summary>
    public BunitContext Build()
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var theBus = ResolveBus();
        var uiBus = new StubUiBus();
        ctx.Services.AddSingleton(theBus);
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(_currentUser, _loggedInAs));
        ctx.Services.AddSingleton<IUiBus>(uiBus);
        ctx.Services.AddSingleton(new WorkOrderBulkActionService(theBus, uiBus));
        ctx.Services.AddSingleton(TimeProvider.System);
        ctx.Services.AddSingleton<ChurchCalendar>();
        ctx.Services.AddSingleton(_searchState ?? new WorkOrderSearchState());
        ctx.Services.AddSingleton<IFeatureFlags>(_featureFlags ?? new StubFeatureFlags());
        RegisterAuth(ctx);
        return ctx;
    }

    private IBus ResolveBus()
    {
        if (_bus != null)
        {
            return _bus;
        }

        return _orders != null ? new StubBus(_orders) : new StubBus();
    }

    private void RegisterAuth(BunitContext ctx)
    {
        var store = new StubUserSessionStore { Username = _loggedInAs };
        var authProvider = new CustomAuthenticationStateProvider(store);
        authProvider.Login(_loggedInAs).GetAwaiter().GetResult();
        ctx.Services.AddSingleton<AuthenticationStateProvider>(authProvider);
    }
}
