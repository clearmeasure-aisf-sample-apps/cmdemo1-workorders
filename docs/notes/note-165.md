# Note 165

`WorkOrder.IsWaitingOnVendor` in `src/Core/Model/WorkOrder.cs` is true only when the status is InProgress and `WaitingOnVendorSince` is not null (`public bool IsWaitingOnVendor => Status == WorkOrderStatus.InProgress && WaitingOnVendorSince != null`). The same fact is stated in `docs/domain/waiting-on-vendor.md` and `docs/glossary/waitingonvendor.md`.
