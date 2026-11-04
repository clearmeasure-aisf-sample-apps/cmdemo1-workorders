# Note 311

`WorkOrder.Priority` in `src/Core/Model/WorkOrder.cs` defaults to `WorkOrderPriority.Medium`. `WorkOrderPriority` in `src/Core/Model/WorkOrderPriority.cs` defines `Low = 1`, `Medium = 2`, and `High = 3`. The type comment says the value is stored as a tinyint and that new work orders default to Medium. `src/Database/scripts/Update/033_AddPriorityAndBuildingToWorkOrder.sql` adds `[Priority] tinyint NOT NULL` with default `2`.
