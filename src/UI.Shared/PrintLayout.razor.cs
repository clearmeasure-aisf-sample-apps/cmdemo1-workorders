using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>Print layout: copyright year comes from the injected clock.</summary>
public partial class PrintLayout
{
    [Inject]
    private TimeProvider Clock { get; set; } = null!;

    /// <summary>Calendar year shown in the print copyright line (UTC).</summary>
    protected int CopyrightYear => Clock.GetUtcNow().Year;
}
