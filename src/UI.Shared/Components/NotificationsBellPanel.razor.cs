using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

/// <summary>
/// Renders the notifications panel for the cascading <see cref="NotificationsBell"/>.
/// </summary>
public partial class NotificationsBellPanel
{
    [CascadingParameter]
    public NotificationsBell Bell { get; set; } = null!;

    protected override void OnInitialized()
    {
        Bell.Changed += OnBellChanged;
    }

    private void OnBellChanged() => InvokeAsync(StateHasChanged);

    public override void Dispose()
    {
        Bell.Changed -= OnBellChanged;
        base.Dispose();
    }
}
