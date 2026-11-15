using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Header control that toggles the cascading <see cref="NavRail"/>.
/// </summary>
public partial class NavRailToggle
{
    [CascadingParameter]
    public NavRail Rail { get; set; } = null!;

    private ElementReference _buttonRef;

    protected override void OnInitialized()
    {
        Rail.Changed += OnRailChanged;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Rail.BindToggleButton(_buttonRef);
    }

    private void OnRailChanged() => InvokeAsync(StateHasChanged);

    private Task OnToggleAsync() => Rail.ToggleAsync();

    public override void Dispose()
    {
        Rail.Changed -= OnRailChanged;
        base.Dispose();
    }
}
