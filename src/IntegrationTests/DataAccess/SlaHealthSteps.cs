using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

/// <summary>Seed steps for SLA health handler tests. Not a fixture.</summary>
internal static class SlaHealthSteps
{
    /// <summary>
    /// Builds one completion an hour into the two-month church window and twelve outside it.
    /// </summary>
    public static WorkOrder[] BuildWindowOrders(Employee creator, DateOnly today, ChurchInstantRange window)
    {
        var inWindow = WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today).CompletedAt(window.Start.AddHours(1)).Build();
        var outside = Enumerable.Range(0, 12).Select(index => WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(TestData.Number()).DueOn(today.AddYears(-3))
            .CompletedAt(window.Start.AddYears(-2).AddDays(index)).Build()).ToArray();
        return [inWindow, ..outside];
    }
}
