using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/admin/buildings")]
[Authorize]
public partial class Buildings : AppComponentBase
{
    private BuildingSummary[]? _data;

    protected override async Task OnInitializedAsync()
    {
        _data = await Bus.Send(new BuildingRoomSummaryQuery());
    }
}
