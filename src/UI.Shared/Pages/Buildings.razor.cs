using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

public partial class Buildings : AppComponentBase
{
    private BuildingSummary[]? _data;

    protected override async Task OnInitializedAsync()
    {
        _data = await Bus.Send(new BuildingRoomSummaryQuery());
    }
}
