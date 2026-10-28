# Cancelled

**Cancelled** is a terminal work-order status: the work order will not be completed and cannot leave this status. Its code is `CNL`, its key is `Cancelled`, and its friendly name is "Cancelled" (`WorkOrderStatus.Cancelled` in [`src/Core/Model/WorkOrderStatus.cs`](../../src/Core/Model/WorkOrderStatus.cs)). The creator cancels from Assigned (`AssignedToCancelledCommand`), from active InProgress (`InProgressToCancelledCommand`), or while waiting on a vendor (`WaitingOnVendorToCancelledCommand`) ([Work order statuses](../domain/work-order-statuses.md)).
