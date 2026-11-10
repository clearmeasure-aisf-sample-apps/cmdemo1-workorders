namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// The status a state command started from, who ran it, when, and the reason a shelve gave. A work order whose
/// status moved away from <see cref="From"/> gets one <see cref="WorkOrderStatusChange"/> history row.
/// </summary>
public sealed record WorkOrderStatusTransition(WorkOrderStatus From, Employee ChangedBy, DateTime ChangedDate, string? Reason)
{
    public static WorkOrderStatusTransition Of(StateCommandBase command, WorkOrderStatus from, DateTime changedDate) =>
        new(from, command.CurrentUser, changedDate, ShelveReason(command));

    /// <summary>The history row for <paramref name="workOrder"/>, or <c>null</c> when its status did not change.</summary>
    public WorkOrderStatusChange? ChangeOf(WorkOrder workOrder) =>
        workOrder.Status == From
            ? null
            : new WorkOrderStatusChange
            {
                Id = Guid.NewGuid(),
                WorkOrderId = workOrder.Id,
                FromStatus = From,
                ToStatus = workOrder.Status,
                ChangedById = ChangedBy.Id,
                ChangedDate = ChangedDate,
                Reason = Reason
            };

    private static string? ShelveReason(StateCommandBase command) =>
        command is InProgressToAssignedCommand { Reason: { } reason } && reason.Trim() is { Length: > 0 } trimmed
            ? trimmed
            : null;
}
