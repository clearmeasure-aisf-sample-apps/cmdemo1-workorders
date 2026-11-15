using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Header bell button for the cascading <see cref="NotificationsBell"/>.
/// </summary>
public partial class NotificationsBellToggle
{
    [CascadingParameter]
    public NotificationsBell Bell { get; set; } = null!;

    private ElementReference _bellButtonRef;

    protected override void OnInitialized()
    {
        Bell.Changed += OnBellChanged;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Bell.BellButtonRef = _bellButtonRef;
    }

    private void OnBellChanged() => InvokeAsync(StateHasChanged);

    private Task OnOpenAsync() => Bell.OpenPanelAsync();

    public override void Dispose()
    {
        Bell.Changed -= OnBellChanged;
        base.Dispose();
    }
}
