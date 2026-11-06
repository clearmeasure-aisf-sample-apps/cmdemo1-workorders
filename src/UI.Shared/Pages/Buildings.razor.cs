using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

public partial class Buildings : AppComponentBase
{
    private BuildingSummary[]? _data;

    private bool _unavailable;

    [Inject] private IFeatureFlags FeatureFlags { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (!await FeatureFlags.IsEnabledAsync(FeatureFlagNames.BuildingManagement))
        {
            _unavailable = true;
            return;
        }

        _data = await Bus.Send(new BuildingRoomSummaryQuery());
    }
}
