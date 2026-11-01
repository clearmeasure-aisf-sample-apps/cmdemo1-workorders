# Note 135

RemoveAttachmentCommand.CanRemove returns true only when the employee is the work order's creator or assignee and the status is Draft, Assigned, or InProgress (`IsCreatorOrAssignee(workOrder, user) && IsRemovableStatus(workOrder.Status)`). RemovableStatuses is Draft, Assigned, and InProgress. IsCreatorOrAssignee is true when the user is WorkOrder.Creator or WorkOrder.Assignee. The method is `RemoveAttachmentCommand.CanRemove` in `src/Core/Model/StateCommands/RemoveAttachmentCommand.cs`.
