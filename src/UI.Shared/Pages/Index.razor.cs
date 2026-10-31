using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// The dashboard: summary and status cards linking to Work Order Search, recent activity and quick links.
/// </summary>
public partial class Index : AppComponentBase, IListener<WorkOrderChangedEvent>
{
    private const int RecentCount = 10;

    private static readonly string MyQueueHref =
        WorkOrderSearchUrlState.ForCard(SummaryCard.AssignedToMe).ToRelativeUri();

    private string? _userName;
    private bool _canCreate;
    private WorkOrderSearchSummary? _summary;
    private Dictionary<string, int>? _statusCounts;
    private WorkOrder[] _recent = [];

    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] private IUserSession UserSession { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _userName = (await AuthStateProvider.GetAuthenticationStateAsync()).User.Identity?.Name;
        if (_userName == null)
        {
            return;
        }

        _canCreate = await CanCreateAsync();
        await LoadAsync();
    }

    public void Handle(WorkOrderChangedEvent theEvent)
    {
        if (_userName != null)
        {
            InvokeAsync(LoadAsync);
        }
    }

    private async Task<bool> CanCreateAsync() =>
        (await UserSession.GetCurrentUserAsync())?.CanCreateWorkOrder() == true;

    private async Task LoadAsync()
    {
        _summary = await Bus.Send(new WorkOrderSearchSummaryQuery(_userName));
        _statusCounts = await Bus.Send(new WorkOrderCountByStatusQuery());
        _recent = await Bus.Send(new RecentWorkOrdersQuery(RecentCount));
        StateHasChanged();
    }

    private static string StatusHref(WorkOrderStatus status) =>
        new WorkOrderSearchUrlState { Status = status.Key }.ToRelativeUri();

    private static string ActivityText(WorkOrder order)
    {
        var activity = WorkOrderActivity.Latest(order);
        var when = activity == null
            ? ""
            : $"{activity.Verb} {activity.ChurchDate.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)} · ";
        return when + order.Status.FriendlyName;
    }
}
