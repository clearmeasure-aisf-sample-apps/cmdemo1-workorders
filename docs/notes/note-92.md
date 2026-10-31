# Note 92

WorkOrder.FriendlyStatus returns GetTextForStatus(), which returns Status.ToString(). WorkOrderStatus.ToString returns FriendlyName, so an Assigned work order's friendly status is "Assigned". The property is `WorkOrder.FriendlyStatus` in `src/Core/Model/WorkOrder.cs`; `WorkOrderStatus.ToString` is in `src/Core/Model/WorkOrderStatus.cs`.
