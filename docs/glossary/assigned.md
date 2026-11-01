# Assigned

**Assigned** is a work order status for a work order that is ready for fulfillment. Its code is `ASD`, its key is `Assigned`, and its friendly name is "Assigned" (`WorkOrderStatus.Assigned` in [`src/Core/Model/WorkOrderStatus.cs`](../../src/Core/Model/WorkOrderStatus.cs)). Assigning moves a Draft work order to Assigned; beginning work moves it to InProgress, and the creator can cancel it ([Work order statuses](../domain/work-order-statuses.md)).
