# Note 93

WorkOrder.ChangeStatus(Employee, DateTime, WorkOrderStatus) sets Status to the given status and, when that status is not WorkOrderStatus.InProgress, sets WaitingOnVendorSince to null (`Status = status` and `if (status != WorkOrderStatus.InProgress) { WaitingOnVendorSince = null; }`). The method is `WorkOrder.ChangeStatus` in `src/Core/Model/WorkOrder.cs`.
