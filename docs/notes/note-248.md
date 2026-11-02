# Note 248

`WorkOrder.IsOpen()` in `src/Core/Model/WorkOrder.cs` returns true unless `Status` is `WorkOrderStatus.Complete` or `WorkOrderStatus.Cancelled` (`public bool IsOpen() => Status != WorkOrderStatus.Complete && Status != WorkOrderStatus.Cancelled;`).
