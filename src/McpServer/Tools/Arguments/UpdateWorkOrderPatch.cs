using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.McpServer.Tools.Arguments;

/// <summary>
/// The planning fields update-work-order may change on a draft. An omitted (null) field is left unchanged; a blank
/// building, room or due date clears it; a blank priority is an error.
/// </summary>
internal sealed record UpdateWorkOrderPatch(
    string? Priority,
    string? Building,
    RoomNumberPatch RoomNumber,
    DueDatePatch DueDate)
{
    public string? ErrorFor(WorkOrder workOrder, Employee executingUser) =>
        DraftEditDenial.ForUpdate(workOrder, executingUser)
        ?? PriorityError()
        ?? BuildingArgument.Parse(Building).Error
        ?? DueDate.Error;

    /// <summary>Applies the patch and returns the Save command, which records the due date it replaced.</summary>
    public SaveDraftCommand ApplyAsSave(WorkOrderRequestContext context)
    {
        var save = new SaveDraftCommand(context.WorkOrder, context.ExecutingUser) { PreviousDueDate = context.WorkOrder.DueDate };
        ApplyTo(context.WorkOrder);
        return save;
    }

    public void ApplyTo(WorkOrder workOrder)
    {
        workOrder.Priority = Priority == null ? workOrder.Priority : PriorityArgument.Parse(Priority).Value;
        workOrder.Building = Building == null ? workOrder.Building : BuildingArgument.Parse(Building).Value;
        RoomNumber.ApplyTo(workOrder);
        DueDate.ApplyTo(workOrder);
    }

    private string? PriorityError() =>
        Priority != null && string.IsNullOrWhiteSpace(Priority)
            ? "Priority cannot be cleared; omit it to leave unchanged."
            : PriorityArgument.Parse(Priority).Error;
}
