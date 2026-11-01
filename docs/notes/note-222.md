# Note 222

`WorkOrder.CanReassign()` in `src/Core/Model/WorkOrder.cs` returns true only when `Status` is `WorkOrderStatus.Draft` (`return Status == WorkOrderStatus.Draft;`). `docs/domain/assignment.md` states that `WorkOrder.CanReassign()` returns true only while the work order is in Draft.
