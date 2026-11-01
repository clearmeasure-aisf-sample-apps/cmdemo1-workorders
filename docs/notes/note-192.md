# Note 192

A new work order's priority defaults to Medium. `WorkOrder.Priority` in `src/Core/Model/WorkOrder.cs` is initialized to `WorkOrderPriority.Medium`, and `WorkOrderPriority.Medium` in `src/Core/Model/WorkOrderPriority.cs` is `2`. `src/Database/scripts/Update/033_AddPriorityAndBuildingToWorkOrder.sql` adds `[Priority] tinyint NOT NULL` with default `2`. `docs/work-order-priorities.md` states that Medium is the default for new work orders.
