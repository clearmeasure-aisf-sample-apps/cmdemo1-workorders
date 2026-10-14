using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns buildings with their rooms and work order counts (open, overdue, completed in last 30 days).
/// Each building contains a list of rooms, and "No building" is included for work orders without a building.
/// </summary>
public record WorkOrderBuildingsAndRoomsQuery : IRequest<BuildingRoomData[]>, IRemotableRequest;

/// <summary>A building with its rooms and work order counts.</summary>
public record BuildingRoomData
{
    public required string BuildingName { get; init; }
    public required RoomData[] Rooms { get; init; }
}

/// <summary>A room with work order counts.</summary>
public record RoomData
{
    public required string RoomName { get; init; }
    public required int OpenCount { get; init; }
    public required int OverdueCount { get; init; }
    public required int CompletedLast30DaysCount { get; init; }
}
