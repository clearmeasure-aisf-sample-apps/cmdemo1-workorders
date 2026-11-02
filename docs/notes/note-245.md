# Note 245

`WorkOrder.CanReassign()` in `src/Core/Model/WorkOrder.cs` returns true only when `Status` is `WorkOrderStatus.Draft` (`return Status == WorkOrderStatus.Draft;`). The same rule is stated in `docs/notes/note-222.md`, `docs/notes/note-203.md`, and `docs/domain/assignment.md`.
