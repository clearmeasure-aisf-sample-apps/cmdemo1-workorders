# Due date

A **due date** is the optional calendar date by which a work order should be completed (`WorkOrder.DueDate`, a nullable date with no time, in [`src/Core/Model/WorkOrder.cs`](../../src/Core/Model/WorkOrder.cs)). A work order with no due date has no due-date urgency. Urgency for an open work order (due today, overdue, or on track) uses the current date in America/Chicago and is described in [Work order due dates](../due-dates.md).
