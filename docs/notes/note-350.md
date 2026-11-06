# Note 350

`WorkOrder.Priority` in `src/Core/Model/WorkOrder.cs` defaults to `WorkOrderPriority.Medium` (`public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;`). `src/Core/Model/WorkOrderPriority.cs` defines `Medium = 2`. The same default is stated in `docs/notes/note-345.md` and `docs/notes/note-42.md`.
