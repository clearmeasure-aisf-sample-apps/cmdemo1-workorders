using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Shell notifications state: badge counts, panel open/close, refresh on navigation or auth change.
/// </summary>
public partial class NotificationsBell
{
    [Inject]
    private IUserSession UserSession { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool _panelOpen;
    private NotificationsSummary? _summary;

    /// <summary>
    /// Raised when badge or panel open state changes so cascaded chrome can re-render.
    /// </summary>
    // ReSharper disable once EventNeverSubscribedTo.Global -- Toggle and Panel subscribe at runtime
    internal event Action? Changed;

    internal bool IsPanelOpen => _panelOpen;

    internal NotificationsSummary? Summary => _summary;

    internal ElementReference BellButtonRef { get; set; }

    internal int BadgeCount => _summary?.OverdueAndDueToday.Length ?? 0;

    internal string NotificationsButtonLabel =>
        BadgeCount > 0 ? $"Notifications ({BadgeCount} unread)" : "Notifications";

    protected override async Task OnInitializedAsync()
    {
        Navigation.LocationChanged += OnLocationChanged;
        UserSession.AuthenticationStateChanged += OnUserSessionChanged;
        await LoadAsync();
        NotifyChanged();
    }

    internal async Task OpenPanelAsync()
    {
        if (await LoadAsync())
        {
            _panelOpen = true;
            NotifyChanged();
        }
    }

    private async Task<bool> LoadAsync()
    {
        try
        {
            var currentUserName = await UserSession.GetCurrentUserNameAsync();
            if (string.IsNullOrWhiteSpace(currentUserName))
            {
                _summary = null;
                return false;
            }

            _summary = await Bus.Send(new NotificationsPanelQuery(currentUserName));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(CloseAndRefreshAsync);

    private void OnUserSessionChanged() =>
        InvokeAsync(RefreshAsync);

    private async Task CloseAndRefreshAsync()
    {
        await ClosePanelAsync();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await LoadAsync();
        NotifyChanged();
    }

    internal async Task ClosePanelAsync()
    {
        _panelOpen = false;
        NotifyChanged();
        await Task.CompletedTask;
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
        StateHasChanged();
    }

    public override void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        UserSession.AuthenticationStateChanged -= OnUserSessionChanged;
        base.Dispose();
    }
}
