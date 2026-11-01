# Draft

**Draft** is a work order status for a work order that has been created and not yet assigned. Its code is `DRT`, its key is `Draft`, and its friendly name is "Draft" (`WorkOrderStatus.Draft` in [`src/Core/Model/WorkOrderStatus.cs`](../../src/Core/Model/WorkOrderStatus.cs)). Saving keeps the work order in Draft; assigning moves it to Assigned ([Work order statuses](../domain/work-order-statuses.md)).
