using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Returns buildings with their rooms and work order counts grouped by building and room.
/// Includes "No building" and "No room" labels for work orders without a building or room.
/// </summary>
public class WorkOrderBuildingsAndRoomsQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<WorkOrderBuildingsAndRoomsQuery, BuildingRoomData[]>
{
    private const int CompletedWindowDays = 30;
    private const string NoBuildingLabel = "No building";
    private const string NoRoomLabel = "No room";

    private sealed record OrderRow(string BuildingName, string RoomName, WorkOrderStatus Status, DateOnly? DueDate, DateTime? CompletedDate);

    public async Task<BuildingRoomData[]> Handle(WorkOrderBuildingsAndRoomsQuery request,
        CancellationToken cancellationToken = default)
    {
        var orders = context.Set<WorkOrder>().AsNoTracking();
        var today = ChurchTimeZone.Today(time);

        var allOrders = await orders
            .Select(wo => new OrderRow(
                wo.Building ?? string.Empty,
                wo.RoomNumber ?? string.Empty,
                wo.Status,
                wo.DueDate,
                wo.CompletedDate))
            .ToListAsync(cancellationToken);

        return allOrders
            .GroupBy(wo => wo.BuildingName)
            .OrderBy(g => EmptyFirst(g.Key))
            .ThenBy(g => Label(g.Key, NoBuildingLabel), StringComparer.OrdinalIgnoreCase)
            .Select(g => ToBuilding(g, today))
            .ToArray();
    }

    private BuildingRoomData ToBuilding(IGrouping<string, OrderRow> buildingGroup, DateOnly today) => new()
    {
        BuildingName = Label(buildingGroup.Key, NoBuildingLabel),
        Rooms = buildingGroup
            .GroupBy(wo => wo.RoomName)
            .OrderBy(g => EmptyFirst(g.Key))
            .ThenBy(g => Label(g.Key, NoRoomLabel), StringComparer.OrdinalIgnoreCase)
            .Select(g => ToRoom(g, today))
            .ToArray()
    };

    private RoomData ToRoom(IGrouping<string, OrderRow> roomGroup, DateOnly today) => new()
    {
        RoomName = Label(roomGroup.Key, NoRoomLabel),
        OpenCount = CountOpen(roomGroup),
        OverdueCount = CountOverdue(roomGroup, today),
        CompletedLast30DaysCount = CountCompletedWithin(roomGroup, time.GetUtcNow().UtcDateTime, CompletedWindowDays)
    };

    private static int EmptyFirst(string key) => key == string.Empty ? 0 : 1;

    private static string Label(string key, string emptyLabel) => key == string.Empty ? emptyLabel : key;

    private static int CountOpen(IEnumerable<OrderRow> orders)
    {
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;
        return orders.Count(wo => wo.Status == draft || wo.Status == assigned || wo.Status == inProgress);
    }

    private static int CountOverdue(IEnumerable<OrderRow> orders, DateOnly today)
    {
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;
        return orders.Count(wo =>
            (wo.Status == draft || wo.Status == assigned || wo.Status == inProgress) &&
            wo.DueDate < today);
    }

    private static int CountCompletedWithin(IEnumerable<OrderRow> orders, DateTime now, int days)
    {
        var complete = WorkOrderStatus.Complete;
        var since = now.AddDays(-days);
        return orders.Count(wo =>
            wo.Status == complete &&
            wo.CompletedDate.HasValue &&
            wo.CompletedDate >= since);
    }
}
