using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// One source for work-order field length and required-field messages. Validators, the manage form,
/// MCP tools and CSV import all read these constants so a limit change stays in Core.
/// Numbers in the message text match <see cref="WorkOrder"/> / shelve max-length constants; characterization
/// tests fail when a limit changes without updating the matching message.
/// </summary>
public static class WorkOrderFieldRules
{
    /// <summary>User-visible message when <see cref="WorkOrder.Title"/> is too long.</summary>
    public const string TitleCannotExceed = "Title cannot exceed 600 characters.";

    /// <summary>User-visible message when <see cref="WorkOrder.Description"/> is too long.</summary>
    public const string DescriptionCannotExceed = "Description cannot exceed 4000 characters.";

    /// <summary>User-visible message when <see cref="WorkOrder.Instructions"/> is too long.</summary>
    public const string InstructionsCannotExceed = "Instructions cannot exceed 4000 characters.";

    /// <summary>User-visible message when <see cref="WorkOrder.Building"/> is too long.</summary>
    public const string BuildingCannotExceed = "Building cannot exceed 100 characters.";

    /// <summary>
    /// CSV import wording for an over-long building (kept distinct from <see cref="BuildingCannotExceed"/>).
    /// </summary>
    public const string BuildingMustBeAtMost = "Building must be at most 100 characters.";

    /// <summary>User-visible message when <see cref="WorkOrder.Floor"/> is too long.</summary>
    public const string FloorCannotExceed = "Floor cannot exceed 20 characters.";

    /// <summary>User-visible message when a changed room number entry is too long.</summary>
    public const string RoomNumberCannotExceed = "Room number cannot exceed 50 characters.";

    /// <summary>User-visible message when a shelve reason is too long.</summary>
    public const string ReasonCannotExceed = "Reason cannot exceed 200 characters.";

    /// <summary>Manage-page and FluentValidation message when shelving without a reason.</summary>
    public const string ShelveReasonRequired = "A reason is required to shelve a work order.";

    /// <summary>MCP shelve-tool wording when a reason is missing (kept byte-identical to prior MCP text).</summary>
    public const string ShelveRequiresReason = "Shelve requires a reason.";

    /// <summary>MCP create-work-order message when description is blank.</summary>
    public const string DescriptionRequired = "Description is required.";

    /// <summary>MCP save-work-order message when a title patch is blank.</summary>
    public const string TitleFieldRequired = "The Title field is required.";

    /// <summary>MCP edit tools message when the work-order number is blank.</summary>
    public const string WorkOrderNumberRequired = "Work order number is required.";

    /// <summary>MCP create-work-order tool description; embeds <see cref="WorkOrder.BuildingMaxLength"/>.</summary>
    public const string CreateWorkOrderToolDescription =
        "Creates a new draft work order. Requires a title, description, and the username of the creator. Optionally accepts a room number, due date (yyyy-MM-dd), and instructions. Priority values: Low, Medium (default), High (case-insensitive). Building is a short name (max 100 characters).";

    /// <summary>MCP building parameter description.</summary>
    public const string BuildingParameterDescription = "Optional building name; max 100 characters";

    /// <summary>MCP building parameter description when empty clears the field.</summary>
    public const string BuildingParameterClearDescription =
        "Optional building name; max 100 characters; empty clears";

    /// <summary>MCP update-work-order tool description.</summary>
    public const string UpdateWorkOrderToolDescription =
        "Updates priority, building, room, and/or due date on an existing draft work order. Requires the work order number and executing username (the creator). Omitted fields are unchanged; empty building/room/due date clears those fields; empty priority is an error. Priority values: Low, Medium, High (case-insensitive). Building: max 100 characters.";

    /// <summary>MCP execute-work-order-command tool description; embeds shelve reason max length.</summary>
    public const string ExecuteWorkOrderCommandToolDescription =
        "Executes a state command on a work order. Available commands: DraftToAssignedCommand (requires assigneeUsername), AssignedToInProgressCommand, InProgressToAssignedCommand, Shelve, InProgressToCompleteCommand, AssignedToCancelledCommand. Shelve and InProgressToAssignedCommand require a reason of at most 200 characters.";

    /// <summary>MCP shelve reason parameter description.</summary>
    public const string ShelveReasonParameterDescription =
        "Reason for shelving (required for Shelve and InProgressToAssignedCommand, at most 200 characters)";

    /// <summary>True when <paramref name="value"/> is longer than <paramref name="maxLength"/>.</summary>
    private static bool Exceeds(string? value, int maxLength) =>
        value is { Length: var length } && length > maxLength;

    /// <summary>Title length error, or null when within <see cref="WorkOrder.TitleMaxLength"/>.</summary>
    public static string? TitleLengthError(string? title) =>
        Exceeds(title, WorkOrder.TitleMaxLength) ? TitleCannotExceed : null;

    /// <summary>Description length error, or null when within <see cref="WorkOrder.DescriptionMaxLength"/>.</summary>
    public static string? DescriptionLengthError(string? description) =>
        Exceeds(description, WorkOrder.DescriptionMaxLength) ? DescriptionCannotExceed : null;

    /// <summary>Required and length checks for a create-work-order description.</summary>
    public static string? DescriptionRequiredOrLengthError(string? description) =>
        string.IsNullOrWhiteSpace(description) ? DescriptionRequired : DescriptionLengthError(description);

    /// <summary>Building length error for forms and MCP, or null when within the limit.</summary>
    public static string? BuildingLengthError(string? building) =>
        Exceeds(building, WorkOrder.BuildingMaxLength) ? BuildingCannotExceed : null;

    /// <summary>Building length error for CSV import (distinct wording).</summary>
    public static string? BuildingCsvLengthError(string? building) =>
        Exceeds(building, WorkOrder.BuildingMaxLength) ? BuildingMustBeAtMost : null;

    /// <summary>
    /// Room entry length error when the value changed; an unchanged legacy room is allowed.
    /// </summary>
    public static string? RoomNumberEntryLengthError(string? roomNumber, string? previousRoomNumber) =>
        roomNumber != previousRoomNumber && Exceeds(roomNumber, WorkOrder.RoomNumberEntryMaxLength)
            ? RoomNumberCannotExceed
            : null;

    /// <summary>Manage-page shelve reason required and length checks after trim.</summary>
    public static string? ShelveReasonError(string trimmedReason) =>
        trimmedReason.Length == 0
            ? ShelveReasonRequired
            : Exceeds(trimmedReason, InProgressToAssignedCommand.ReasonMaxLength)
                ? ReasonCannotExceed
                : null;

    /// <summary>MCP shelve reason required and length checks after trim.</summary>
    public static string? McpShelveReasonError(string trimmedReason) =>
        trimmedReason.Length == 0
            ? ShelveRequiresReason
            : Exceeds(trimmedReason, InProgressToAssignedCommand.ReasonMaxLength)
                ? ReasonCannotExceed
                : null;

    /// <summary>Save-work-order title patch: blank is refused; over-long uses <see cref="TitleCannotExceed"/>.</summary>
    public static string? TitlePatchError(string? title)
    {
        if (title != null && string.IsNullOrWhiteSpace(title))
        {
            return TitleFieldRequired;
        }

        return TitleLengthError(title);
    }
}
