using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Turns an <see cref="SlaTrendComparison"/> into the SLA card's trend text: "↑ up from X%", "↓ down from X%",
/// "— flat at X%", or null when no comparison is possible.
/// </summary>
internal static class SlaTrendFormatter
{
    internal static string? Format(SlaTrendComparison comparison)
    {
        var percent = comparison.ComparedPercentage;
        return comparison.Trend switch
        {
            SlaTrend.Up => $"↑ up from {percent:0}%",
            SlaTrend.Down => $"↓ down from {percent:0}%",
            SlaTrend.Flat => $"— flat at {percent:0}%",
            _ => null
        };
    }
}
