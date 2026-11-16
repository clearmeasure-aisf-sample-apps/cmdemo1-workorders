namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// Fixed <see cref="TimeProvider"/> for relative-time and overdue styling tests.
/// </summary>
public sealed class WorkOrderManageFixedClock(DateTimeOffset utcNow) : TimeProvider
{
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => utcNow;
}
