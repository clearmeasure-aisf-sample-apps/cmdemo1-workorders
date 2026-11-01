# Note 88

WorkOrder.IsOpen returns true unless the status is Complete or Cancelled (`Status != WorkOrderStatus.Complete && Status != WorkOrderStatus.Cancelled`). The method is `WorkOrder.IsOpen` in `src/Core/Model/WorkOrder.cs`.
