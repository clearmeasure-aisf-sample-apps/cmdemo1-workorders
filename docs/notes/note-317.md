# Note 317

`WorkOrder.IsOpen` in `src/Core/Model/WorkOrder.cs` returns true unless the status is Complete or Cancelled (`Status != WorkOrderStatus.Complete && Status != WorkOrderStatus.Cancelled`). The XML comment on that method states the same fact.
