using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UI.Shared;

public partial class MainLayout : IAsyncDisposable
{
    /// <summary>
    /// Must stay aligned with <c>@media (max-width: 768px)</c> in <c>MainLayout.razor.css</c> and the
    /// <c>matchMedia</c> argument in <c>mainLayoutNav.js</c>.
    /// </summary>
    public const string NavRailBreakpointMediaQuery = "(max-width: 768px)";

    public enum Elements
    {
        NavRailToggle,
        CopyrightFooter,
        FooterNote,
        SoftwareVersion,
        DarkModeToggle,
        GitSha,
        EnvironmentName,
        BrandSubtitle,
        HelpLink,
        NotificationsButton,
        NotificationsBadge
    }

    /// <summary>
    /// Calendar year shown in the site copyright line (UTC, matches acceptance tests).
    /// </summary>
    protected int CopyrightYear => DateTime.UtcNow.Year;

    /// <summary>
    /// Informational version of the running entry assembly, e.g. "1.2.3+abc1234".
    /// </summary>
    protected string AppVersion =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;

    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    [Inject]
    private ThemePreferenceService Theme { get; set; } = null!;

    [Inject]
    private HttpClient Http { get; set; } = null!;

    [Inject]
    private new IBus Bus { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    private ElementReference _navToggleButtonRef;
    private ElementReference _bellButtonRef;
    private DotNetObjectReference<MainLayout>? _dotNetRef;
    private bool _disposed;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _navToggleHelper;
    private bool _isNarrowViewport;
    private bool _viewportSynced;
    private bool _navVisible = true;
    private string _gitSha = "unknown";
    private string _environmentName = "unknown";
    private bool _notificationsPanelOpen;
    private NotificationsPanel? _notificationsPanelData;

    private string AppContainerClass => NavRailCss.AppContainerClass(_isNarrowViewport, _navVisible);

    private string SidebarClass => NavRailCss.SidebarClass(_isNarrowViewport, _navVisible);

    private string NavToggleTitle =>
        _navVisible ? "Hide navigation panel" : "Show navigation panel";

    private string NavToggleAriaExpanded => _navVisible ? "true" : "false";

    private string DarkModeToggleTitle => Theme.IsDarkMode ? "Switch to light mode" : "Switch to dark mode";

    private int BadgeCount => _notificationsPanelData?.OverdueAndDueToday.Length ?? 0;

    [JSInvokable]
    public Task OnViewportChanged(bool isNarrow)
    {
        if (!_viewportSynced)
        {
            _viewportSynced = true;
            if (isNarrow)
                _navVisible = false;
        }

        _isNarrowViewport = isNarrow;
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        await ApplyBrowserCultureAsync();
        Navigation.LocationChanged += OnLocationChanged;
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;

        try
        {
            var response = await Http.GetAsync("/api/status/environment");
            if (response.IsSuccessStatusCode)
            {
                (_gitSha, _environmentName) = await ParseEnvironmentStatusAsync(response);
            }
        }
        catch (HttpRequestException)
        {
            // graceful fallback — fields remain "unknown"
        }
        catch (JsonException)
        {
            // graceful fallback — malformed response, fields remain "unknown"
        }

        await LoadNotificationsAsync();
    }

    private async Task ApplyBrowserCultureAsync()
    {
        try
        {
            var language = await Js.InvokeAsync<string>("churchBulletinBrowserCulture");
            ClientDateFormatter.ApplyBrowserCulture(language);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
            // The shell still renders when the browser language cannot be read.
        }
    }

    /// <summary>
    /// Extracted from <see cref="OnInitializedAsync"/> to keep that method's cyclomatic
    /// complexity (and therefore its CRAP score) low; the property-presence branching lives
    /// here instead, fully exercised by <c>MainLayoutTests</c>.
    /// </summary>
    private static async Task<(string GitSha, string EnvironmentName)> ParseEnvironmentStatusAsync(
        HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var gitSha = doc.RootElement.TryGetProperty("gitSha", out var sha) ? sha.GetString() ?? "unknown" : "unknown";
        var environmentName = doc.RootElement.TryGetProperty("environmentName", out var env)
            ? env.GetString() ?? "unknown"
            : "unknown";
        return (gitSha, environmentName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await Js.InvokeAsync<IJSObjectReference>("import",
                "./_content/ClearMeasure.Bootcamp.UI.Shared/js/mainLayoutNav.js");
            // A page with its own layout (the print view) replaces this layout while the module loads.
            if (_disposed)
                return;
            _navToggleHelper = await _jsModule.InvokeAsync<IJSObjectReference>("initNavToggle", _dotNetRef,
                NavRailBreakpointMediaQuery);
        }
        catch (JSDisconnectedException)
        {
        }

        try
        {
            await Theme.InitializeAsync();
            Theme.OnChange += OnThemeChanged;
            await InvokeAsync(StateHasChanged);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    private async Task ToggleDarkModeAsync() => await Theme.SetDarkModeAsync(!Theme.IsDarkMode);

    private async Task ToggleNavRailAsync()
    {
        var wasVisible = _navVisible;
        _navVisible = !wasVisible;
        await InvokeAsync(StateHasChanged);

        if (_isNarrowViewport && wasVisible)
        {
            await Task.Yield();
            try
            {
                await _navToggleButtonRef.FocusAsync();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private async Task OpenNotificationsPanelAsync()
    {
        if (await LoadNotificationsAsync())
        {
            _notificationsPanelOpen = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Loads the notifications for the signed-in user so the bell badge is correct on page load.
    /// Returns false when there is no signed-in user or the query fails.
    /// </summary>
    private async Task<bool> LoadNotificationsAsync()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var currentUserName = ResolveUserName(authState.User);
            if (string.IsNullOrWhiteSpace(currentUserName))
            {
                _notificationsPanelData = null;
                return false;
            }

            _notificationsPanelData = await Bus.Send(new NotificationsPanelQuery(currentUserName));
            return true;
        }
        catch (Exception)
        {
            // Silently fail - badge stays hidden and panel won't open
            return false;
        }
    }

    private static string? ResolveUserName(ClaimsPrincipal user) =>
        (user.FindFirst("preferred_username") ?? user.FindFirst("name"))?.Value ?? user.Identity?.Name;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(CloseAndRefreshNotificationsAsync);

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) =>
        InvokeAsync(RefreshNotificationsAsync);

    private async Task CloseAndRefreshNotificationsAsync()
    {
        await CloseNotificationsPanelAsync();
        await RefreshNotificationsAsync();
    }

    private async Task RefreshNotificationsAsync()
    {
        await LoadNotificationsAsync();
        StateHasChanged();
    }

    private async Task CloseNotificationsPanelAsync()
    {
        _notificationsPanelOpen = false;
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        Theme.OnChange -= OnThemeChanged;
        Navigation.LocationChanged -= OnLocationChanged;
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;

        if (_navToggleHelper is not null)
        {
            try
            {
                await _navToggleHelper.InvokeVoidAsync("dispose");
            }
            catch (JSDisconnectedException)
            {
            }

            await _navToggleHelper.DisposeAsync();
        }

        if (_jsModule is not null)
            await _jsModule.DisposeAsync();

        _dotNetRef?.Dispose();
    }
}

