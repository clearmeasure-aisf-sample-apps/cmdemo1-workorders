using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>Seed steps for SLA health handler tests. Not a fixture.</summary>
internal static class SlaHealthSteps
{
    /// <summary>
    /// Builds one completion inside the two-month church window (at <see cref="PointInside"/>) and twelve outside it.
    /// </summary>
    public static WorkOrder[] BuildWindowOrders(Employee creator, DateOnly today, ChurchInstantRange window)
    {
        var inWindow = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today).CompletedAt(PointInside(window)).Build();
        var outside = Enumerable.Range(0, 12).Select(index => WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today.AddYears(-3))
            .CompletedAt(window.Start.AddYears(-2).AddDays(index)).Build()).ToArray();
        return [inWindow, ..outside];
    }

    /// <summary>
    /// An instant guaranteed to be inside <paramref name="window"/>: a quarter of the way through it,
    /// <c>window.Start + (window.EndExcluded - window.Start) / 4</c>, truncated to whole ticks (so it is
    /// <paramref name="window"/>'s <c>Start</c> for a window narrower than four ticks). Use it to name a completion
    /// that must fall inside the window without hard-coding an offset such as one hour after its start.
    /// </summary>
    public static DateTime PointInside(ChurchInstantRange window)
    {
        return window.Start + TimeSpan.FromTicks((window.EndExcluded - window.Start).Ticks / 4);
    }
}
