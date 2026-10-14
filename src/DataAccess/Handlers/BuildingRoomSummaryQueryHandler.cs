using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Counts the open, overdue, recently completed and total work orders per building and room in one
/// SQL statement. Buildings and rooms are grouped and joined by the database (its trimming and
/// collation, the same as the Work Order Search filters), never compared in .NET, and the counts use
/// the predicates of <see cref="WorkOrderQueryFilters"/> that the search and its summary cards use.
/// </summary>
public class BuildingRoomSummaryQueryHandler(DataContext context, TimeProvider time)
    : IRequestHandler<BuildingRoomSummaryQuery, BuildingSummary[]>
{
    private const int CompletedWindowDays = 30;

    public async Task<BuildingSummary[]> Handle(BuildingRoomSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        var counts = await CountsQuery().ToListAsync(cancellationToken);

        var buildings = new List<BuildingSummary>();
        var rooms = new List<RoomSummary>();
        string? anchor = null;
        foreach (var count in counts)
        {
            if (buildings.Count == 0 || count.Anchor != anchor)
            {
                Close(buildings, rooms);
                buildings.Add(new BuildingSummary { Building = count.Building });
                anchor = count.Anchor;
            }

            rooms.Add(new RoomSummary
            {
                Room = count.Room, OpenCount = count.Open, OverdueCount = count.Overdue,
                CompletedLast30Days = count.Completed, TotalCount = count.Total
            });
        }

        Close(buildings, rooms);
        return buildings.ToArray();
    }

    /// <summary>
    /// The grouped counts, one row per building and room, ordered by building then room. Each building
    /// is identified by its anchor: the smallest work order number among the rows the database groups
    /// under that building, so rooms of one building share it even when their spellings differ in case.
    /// </summary>
    internal IQueryable<RoomCount> CountsQuery()
    {
        var rows = context.Set<WorkOrder>().AsNoTracking()
            .Select(WorkOrderQueryFilters.CountRow(ChurchTimeZone.Today(time), time, CompletedWindowDays));
        var buildings = rows
            .GroupBy(r => r.Building)
            .Select(g => new { g.Key, Anchor = g.Min(r => r.Number) });

        return rows
            .Join(buildings, r => r.Building, b => b.Key,
                (r, b) => new { b.Anchor, Building = b.Key, r.Room, r.Open, r.Overdue, r.Completed })
            .GroupBy(x => new { x.Anchor, x.Building, x.Room })
            .Select(g => new RoomCount
            {
                Anchor = g.Key.Anchor,
                Building = g.Key.Building,
                Room = g.Key.Room,
                Open = g.Sum(x => x.Open),
                Overdue = g.Sum(x => x.Overdue),
                Completed = g.Sum(x => x.Completed),
                Total = g.Count()
            })
            .OrderBy(c => c.Building)
            .ThenBy(c => c.Anchor)
            .ThenBy(c => c.Room);
    }

    private static void Close(List<BuildingSummary> buildings, List<RoomSummary> rooms)
    {
        if (buildings.Count > 0 && rooms.Count > 0)
        {
            buildings[^1].Rooms = rooms.ToArray();
            rooms.Clear();
        }
    }

    // ReSharper disable PropertyCanBeMadeInitOnly.Global -- materialized by EF Core from a MemberInit projection
    internal sealed class RoomCount
    {
        public string? Anchor { get; set; }
        public string Building { get; set; } = "";
        public string Room { get; set; } = "";
        public int Open { get; set; }
        public int Overdue { get; set; }
        public int Completed { get; set; }
        public int Total { get; set; }
    }
}
