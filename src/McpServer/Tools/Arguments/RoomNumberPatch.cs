using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// A room-number patch from an MCP tool: omitted (null) leaves the room unchanged, blank stores an empty room, and a
/// stored room is cut to <see cref="WorkOrder.RoomNumberMaxLength"/> characters.
/// </summary>
internal readonly record struct RoomNumberPatch(string? Text)
{
    /// <summary>
    /// A changed entry may not exceed <see cref="WorkOrder.RoomNumberEntryMaxLength"/> characters; resending the
    /// current room is always allowed.
    /// </summary>
    public string? ErrorFor(WorkOrder workOrder) =>
        Text is { Length: > WorkOrder.RoomNumberEntryMaxLength } && Text != workOrder.RoomNumber
            ? $"Room number cannot exceed {WorkOrder.RoomNumberEntryMaxLength} characters."
            : null;

    public void ApplyTo(WorkOrder workOrder)
    {
        if (Text == null)
        {
            return;
        }

        var room = string.IsNullOrWhiteSpace(Text) ? string.Empty : Text;
        workOrder.RoomNumber = room.Length <= WorkOrder.RoomNumberMaxLength ? room : room[..WorkOrder.RoomNumberMaxLength];
    }
}
