using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Removes one attachment from a work order, on behalf of the work order's creator or assignee.</summary>
public record RemoveAttachmentCommand(Guid WorkOrderId, Guid AttachmentId, string UserName)
    : IRequest<RemoveAttachmentResult>, IRemotableRequest
{
    private static readonly WorkOrderStatus[] RemovableStatuses =
        [WorkOrderStatus.Draft, WorkOrderStatus.Assigned, WorkOrderStatus.InProgress];

    /// <summary>True when <paramref name="user"/> may remove attachments from <paramref name="workOrder"/>.</summary>
    public static bool CanRemove(WorkOrder workOrder, Employee user) =>
        IsCreatorOrAssignee(workOrder, user) && IsRemovableStatus(workOrder.Status);

    /// <summary>Returns the reason the removal is refused, or null when it is allowed.</summary>
    public static string? Validate(WorkOrder? workOrder, Employee? user, WorkOrderAttachment? attachment)
    {
        if (workOrder is null) return "Work order not found.";
        if (user is null) return "User not found.";
        if (attachment is null) return "Attachment not found.";
        return ValidateRules(workOrder, user, attachment);
    }

    private static string? ValidateRules(WorkOrder workOrder, Employee user, WorkOrderAttachment attachment)
    {
        if (attachment.WorkOrderId != workOrder.Id) return "Attachment does not belong to this work order.";
        if (!IsCreatorOrAssignee(workOrder, user)) return "Only the creator or the assignee can remove attachments.";
        if (!IsRemovableStatus(workOrder.Status))
            return "Attachments cannot be removed from a Complete or Cancelled work order.";
        return null;
    }

    private static bool IsCreatorOrAssignee(WorkOrder workOrder, Employee user) =>
        user == workOrder.Creator || user == workOrder.Assignee;

    private static bool IsRemovableStatus(WorkOrderStatus status) => RemovableStatuses.Contains(status);
}
