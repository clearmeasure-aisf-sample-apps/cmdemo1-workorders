# InProgress

**InProgress** is a work order status for work that is actively underway. Its code is `IPG`, its key is `InProgress`, and its friendly name is "In Progress" (`WorkOrderStatus.InProgress` in [`src/Core/Model/WorkOrderStatus.cs`](../../src/Core/Model/WorkOrderStatus.cs)). Waiting on a vendor is a substate of InProgress, not a separate status: waiting and resuming do not change the persisted status ([Work order statuses](../domain/work-order-statuses.md)).
