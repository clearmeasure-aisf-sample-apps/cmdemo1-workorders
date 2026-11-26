namespace ClearMeasure.Bootcamp.Core.Services;

/// <summary>
/// Direction of this month's SLA on-time percentage compared with last month's.
/// </summary>
public enum SlaTrend
{
    /// <summary>No comparison is possible because a month has no data.</summary>
    Unknown,

    /// <summary>This month's percentage is higher than last month's.</summary>
    Up,

    /// <summary>This month's percentage is lower than last month's.</summary>
    Down,

    /// <summary>This month's percentage equals last month's.</summary>
    Flat
}
