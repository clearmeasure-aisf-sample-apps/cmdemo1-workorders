using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>Seed steps for work-order reports handler tests. Not a fixture.</summary>
internal static class WorkOrderReportsSteps
{
    /// <summary>
    /// Builds one completion in each of the last eight weeks plus two at day -56 and one open draft.
    /// </summary>
    public static WorkOrder[] BuildCompletedWeekOrders(
        Employee creator, DateTime now, DateTimeOffset seedNow, DateOnly today) =>
    [
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-56)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-56)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-49)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-42)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-35)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-28)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-21)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-14)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now.AddDays(-7)).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).CreatedAt(seedNow.DateTime).DueOn(today)
            .CompletedAt(now).Build(),
        WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).CreatedAt(seedNow.DateTime).DueOn(today).Build()
    ];
}
