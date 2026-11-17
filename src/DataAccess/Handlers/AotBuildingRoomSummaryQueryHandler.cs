using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Mediator;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// <see cref="BuildingRoomSummaryQueryHandler"/> for the UI.Server Native AOT build: buildings and rooms grouped by
/// their trimmed names ignoring case (as the database groups them for the JIT handler), counted over the
/// <see cref="WorkOrderSnapshot"/> with the same predicates.
/// </summary>
public class AotBuildingRoomSummaryQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<BuildingRoomSummaryQuery, BuildingSummary[]>
{
    private readonly WorkOrderSnapshotSource _source = new(context, time);

    private const int CompletedWindowDays = 30;
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public async ValueTask<BuildingSummary[]> Handle(BuildingRoomSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _source.LoadAsync(cancellationToken);
        var completed = snapshot.IsCompletedWithin(CompletedWindowDays);
        return snapshot.All
            .GroupBy(wo => (wo.Building ?? "").Trim(), KeyComparer)
            .OrderBy(building => building.Key, KeyComparer)
            .Select(building => new BuildingSummary
            {
                Building = building.Key,
                Rooms = building
                    .GroupBy(wo => (wo.RoomNumber ?? "").Trim(), KeyComparer)
                    .OrderBy(room => room.Key, KeyComparer)
                    .Select(room => new RoomSummary
                    {
                        Room = room.Key,
                        OpenCount = room.Count(WorkOrderSnapshot.IsOpen),
                        OverdueCount = room.Count(snapshot.IsOverdue),
                        CompletedLast30Days = room.Count(completed),
                        TotalCount = room.Count()
                    })
                    .ToArray()
            })
            .ToArray();
    }
}
