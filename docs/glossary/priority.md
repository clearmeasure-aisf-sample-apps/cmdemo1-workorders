# Priority

**Priority** is how urgently a work order should be handled (`WorkOrder.Priority`, type `WorkOrderPriority`, in [`src/Core/Model/WorkOrder.cs`](../../src/Core/Model/WorkOrder.cs) and [`src/Core/Model/WorkOrderPriority.cs`](../../src/Core/Model/WorkOrderPriority.cs)). The values are Low (1), Medium (2), and High (3); new work orders default to Medium, and the value is stored as a tinyint. Work-order search can filter by one priority, and omitting the filter does not restrict the results ([Work order priorities](../domain/work-order-priorities.md)).
