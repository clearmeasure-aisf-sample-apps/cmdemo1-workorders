# Work order assignment

A Draft work order becomes Assigned through `DraftToAssignedCommand` (present-tense verb "Assign"). Only the creator can execute the command. Execution sets `AssignedDate` from the transition time, then moves the status from Draft to Assigned. When the assignee holds the Bot role, the command raises `WorkOrderAssignedToBotEvent`.

`WorkOrder.CanReassign()` returns true only when the status is Draft. The manage form disables the assignee selector when `CanReassign()` is false, so the assignee can be chosen or changed only while the work order is Draft.
