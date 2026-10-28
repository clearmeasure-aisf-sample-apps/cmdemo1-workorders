# Complete

**Complete** is a terminal work-order status: the work has been finished and the work order cannot leave this status. Its code is `CMP`, its key is `Complete`, and its friendly name is "Complete" (`WorkOrderStatus.Complete` in [`src/Core/Model/WorkOrderStatus.cs`](../../src/Core/Model/WorkOrderStatus.cs)). The assignee finishes an InProgress work order with `InProgressToCompleteCommand`, which sets `CompletedDate` and moves the status to Complete ([Work order statuses](../domain/work-order-statuses.md)).
