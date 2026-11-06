# Note 360

`WorkOrder.Priority` in `src/Core/Model/WorkOrder.cs` defaults to `WorkOrderPriority.Medium` (`public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium`). `WorkOrderPriority.Medium` in `src/Core/Model/WorkOrderPriority.cs` is stored as 2, and its comment says "Normal handling; the default for new work orders."
