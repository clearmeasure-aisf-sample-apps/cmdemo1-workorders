using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// The fields save-work-order may change on a draft. An omitted (null) field is left unchanged; a blank
/// description, instructions or room is stored empty; a blank due date clears it; a blank title is an error.
/// </summary>
internal sealed record SaveWorkOrderPatch(
    string? Title,
    string? Description,
    string? Instructions,
    RoomNumberPatch RoomNumber,
    DueDatePatch DueDate)
{
    public string? ErrorFor(WorkOrder workOrder, Employee executingUser) =>
        DraftEditDenial.ForSave(workOrder, executingUser)
        ?? TitleError()
        ?? DescriptionError()
        ?? RoomNumber.ErrorFor(workOrder)
        ?? DueDate.Error;

    /// <summary>Applies the patch and returns the Save command, which records the room and due date it replaced.</summary>
    public SaveDraftCommand ApplyAsSave(WorkOrderRequestContext context)
    {
        var save = new SaveDraftCommand(context.WorkOrder, context.ExecutingUser)
        {
            PreviousRoomNumber = context.WorkOrder.RoomNumber,
            PreviousDueDate = context.WorkOrder.DueDate
        };
        ApplyTo(context.WorkOrder);
        return save;
    }

    public void ApplyTo(WorkOrder workOrder)
    {
        workOrder.Title = Title ?? workOrder.Title;
        workOrder.Description = Description == null ? workOrder.Description : BlankToEmpty(Description);
        workOrder.Instructions = Instructions == null ? workOrder.Instructions : BlankToEmpty(Instructions);
        RoomNumber.ApplyTo(workOrder);
        DueDate.ApplyTo(workOrder);
    }

    private string? TitleError()
    {
        if (Title != null && string.IsNullOrWhiteSpace(Title))
        {
            return "The Title field is required.";
        }

        return Title is { Length: > WorkOrder.TitleMaxLength }
            ? $"Title cannot exceed {WorkOrder.TitleMaxLength} characters."
            : null;
    }

    private string? DescriptionError() => NewWorkOrderArguments.DescriptionLengthError(Description);

    private static string BlankToEmpty(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value;
}
