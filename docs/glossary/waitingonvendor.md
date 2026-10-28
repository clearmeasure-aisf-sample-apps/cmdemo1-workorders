# WaitingOnVendor

**WaitingOnVendor** is a substate of InProgress, not a work order status. `WaitingOnVendorSince` is set while the assignee waits on a vendor and is null during active InProgress; `IsWaitingOnVendor` is true only when the status is InProgress and that timestamp is set (`WorkOrder` in [`src/Core/Model/WorkOrder.cs`](../../src/Core/Model/WorkOrder.cs)). The same description is in [Waiting on a Vendor](../domain/waiting-on-vendor.md).
