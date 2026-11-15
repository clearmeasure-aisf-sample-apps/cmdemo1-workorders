using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>Seed steps for building/room summary handler tests. Not a fixture.</summary>
internal static class BuildingRoomSummarySteps
{
    /// <summary>
    /// Builds hall-room variants: case/whitespace building names, open/overdue/complete/in-progress
    /// rows for search-count parity.
    /// </summary>
    public static WorkOrder[] BuildHallOrders(
        Employee creator, string hall, DateTimeOffset now, DateOnly churchToday)
    {
        var nowUtc = now.DateTime;
        var created = now.DateTime.AddDays(-90);
        return
        [
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).InBuilding(hall).InRoom("101")
                .CreatedAt(created).DueOn(churchToday.AddDays(-1)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).InBuilding($" {hall.ToUpperInvariant()} ").InRoom("101 ")
                .CreatedAt(created).DueOn(churchToday).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding(hall.ToLowerInvariant()).InRoom("102")
                .CreatedAt(created).CompletedAt(nowUtc.AddDays(-30)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding(hall.ToLowerInvariant()).InRoom("102")
                .CreatedAt(created).CompletedAt(nowUtc.AddDays(-31)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).InBuilding(hall).InRoom("101")
                .CreatedAt(created).Build()
        ];
    }

    /// <summary>
    /// Builds blank-building and gym-room variants for null/empty/whitespace and gym-tagged rows.
    /// </summary>
    public static WorkOrder[] BuildBlankAndGymOrders(
        Employee creator, string gym, DateTimeOffset now, DateOnly churchToday)
    {
        var nowUtc = now.DateTime;
        var created = now.DateTime.AddDays(-90);
        return
        [
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled)
                .CreatedAt(created).DueOn(churchToday.AddDays(-12)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding("").InRoom("")
                .CreatedAt(created).CompletedAt(nowUtc.AddDays(-60)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Assigned).InBuilding("   ").InRoom("  ")
                .CreatedAt(created).DueOn(churchToday.AddDays(-12)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).InRoom("201").CreatedAt(created).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding("").InRoom("201")
                .CreatedAt(created).CompletedAt(nowUtc.AddDays(-2)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).InBuilding("   ").InRoom(" 201")
                .CreatedAt(created).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.InProgress).InBuilding(gym)
                .CreatedAt(created).DueOn(churchToday.AddDays(-3)).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Cancelled).InBuilding(gym).InRoom("  ")
                .CreatedAt(created).Build(),
            WorkOrderBuilder.For(creator, WorkOrderStatus.Complete).InBuilding($" {gym}").InRoom("")
                .CreatedAt(created).CompletedAt(nowUtc.AddDays(-1)).Build()
        ];
    }
}
