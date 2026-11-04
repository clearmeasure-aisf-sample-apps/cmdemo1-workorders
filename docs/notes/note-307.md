# Note 307

`WorkOrder.CanReassign()` in `src/Core/Model/WorkOrder.cs` returns true only when `Status` is `WorkOrderStatus.Draft` (`return Status == WorkOrderStatus.Draft;`). `docs/domain/assignment.md` states the same rule.
