# Note 203

`WorkOrder.CanReassign()` in `src/Core/Model/WorkOrder.cs` returns true only when `Status` is `WorkOrderStatus.Draft` (`return Status == WorkOrderStatus.Draft;`).
