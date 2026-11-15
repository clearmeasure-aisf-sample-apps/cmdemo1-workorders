using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>Seed steps for SLA health handler tests. Not a fixture.</summary>
internal static class SlaHealthSteps
{
    /// <summary>
    /// Builds one in-window completion and twelve outside the two-month church window.
    /// </summary>
    public static WorkOrder[] BuildWindowOrders(
        Employee creator, DateOnly today, DateTime windowStart, DateTime completed)
    {
        var inWindow = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today).CompletedAt(completed).Build();
        var outside = Enumerable.Range(0, 12).Select(index => WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today.AddYears(-3))
            .CompletedAt(windowStart.AddYears(-2).AddDays(index)).Build()).ToArray();
        return [inWindow, ..outside];
    }
}
