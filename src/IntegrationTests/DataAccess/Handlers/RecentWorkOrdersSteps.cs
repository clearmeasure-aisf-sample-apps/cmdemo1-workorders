using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>Seed steps for recent-work-orders handler tests. Not a fixture.</summary>
internal static class RecentWorkOrdersSteps
{
    /// <summary>Builds thirteen orders with staggered created/completed dates for recency ranking.</summary>
    public static WorkOrder[] BuildLatestChangedOrders(Employee creator, DateTime start, string[] numbers)
    {
        var orders = Enumerable.Range(1, 11)
            .Select(i => WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
                .WithNumber(numbers[i - 1]).WithTitle(numbers[i - 1]).CreatedAt(start.AddDays(10 + i)).Build())
            .ToList();
        orders.Add(WorkOrderBuilder.For(creator, WorkOrderStatus.Complete)
            .WithNumber(numbers[11]).WithTitle(numbers[11])
            .CreatedAt(start).CompletedAt(start.AddDays(40)).Build());
        orders.Add(WorkOrderBuilder.For(creator, WorkOrderStatus.Draft)
            .WithNumber(numbers[12]).WithTitle(numbers[12]).CreatedAt(null).Build());
        return orders.ToArray();
    }
}
