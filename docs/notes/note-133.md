# Note 133

WorkOrder.IsOpen returns true unless the status is Complete or Cancelled (`public bool IsOpen() => Status != WorkOrderStatus.Complete && Status != WorkOrderStatus.Cancelled;`). The summary on the method says the same thing. The method is `WorkOrder.IsOpen` in `src/Core/Model/WorkOrder.cs`.
