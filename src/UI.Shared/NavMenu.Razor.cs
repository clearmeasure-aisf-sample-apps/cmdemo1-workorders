using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Sidebar navigation: Workspace, My Queue (with count badges from
/// <see cref="WorkOrderSearchSummaryQuery"/>) and Administration sections.
/// </summary>
public partial class NavMenu : AppComponentBase,
    IListener<UserLoggedInEvent>, IListener<UserLoggedOutEvent>, IListener<WorkOrderChangedEvent>
{
    [Inject] public IUserSession? UserSession { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private Employee? CurrentUser { get; set; }

    private WorkOrderSearchSummary? Summary { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Navigation.LocationChanged += OnLocationChanged;
        await SetCurrentUser();
        await LoadSummaryAsync();
    }

    private async Task SetCurrentUser()
    {
        CurrentUser = await UserSession!.GetCurrentUserAsync();
    }

    private async Task LoadSummaryAsync()
    {
        Summary = CurrentUser == null
            ? null
            : await Bus.Send(new WorkOrderSearchSummaryQuery(CurrentUser.UserName));
    }

    private bool IsActive(string href) => NavQueueMatcher.IsActive(Navigation.Uri, Navigation.BaseUri, href);

    private string LinkClass(string href, bool danger) =>
        "nav-link" + (danger ? " nav-link--danger" : string.Empty) + (IsActive(href) ? " active" : string.Empty);

    private string? AriaCurrent(string href) => IsActive(href) ? "page" : null;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => InvokeAsync(StateHasChanged);

    public void Handle(UserLoggedInEvent theEvent)
    {
        InvokeAsync(async () =>
        {
            await SetCurrentUser();
            await LoadSummaryAsync();
            StateHasChanged();
        });
    }

    public void Handle(UserLoggedOutEvent theEvent)
    {
        CurrentUser = null;
        Summary = null;
        StateHasChanged();
    }

    public void Handle(WorkOrderChangedEvent theEvent)
    {
        InvokeAsync(async () =>
        {
            await LoadSummaryAsync();
            StateHasChanged();
        });
    }

    public override void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        base.Dispose();
    }
}
