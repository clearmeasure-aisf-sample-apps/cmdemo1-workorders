// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json round-trip serialization (remoting protocol) requires mutable setters
namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// One building returned by <see cref="BuildingRoomSummaryQuery"/> with the work order counts of its rooms.
/// </summary>
public class BuildingSummary
{
    /// <summary>Trimmed building name; empty means "no building" (search with <c>NoBuilding</c>).</summary>
    public string Building { get; set; } = "";

    /// <summary>The rooms of the building that have at least one work order.</summary>
    public RoomSummary[] Rooms { get; set; } = [];
}

/// <summary>
/// Work order counts of one room of a <see cref="BuildingSummary"/>. "Open" means Draft, Assigned or In Progress.
/// </summary>
public class RoomSummary
{
    /// <summary>Trimmed room number; empty means "no room" (search with <c>NoRoom</c>).</summary>
    public string Room { get; set; } = "";

    /// <summary>Open work orders.</summary>
    public int OpenCount { get; set; }

    /// <summary>Open work orders whose due date is before the church's today.</summary>
    public int OverdueCount { get; set; }

    /// <summary>Work orders completed in the last 30 days.</summary>
    public int CompletedLast30Days { get; set; }

    /// <summary>All work orders of the room in any status.</summary>
    public int TotalCount { get; set; }
}
