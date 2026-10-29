# Overdue

**Overdue** is the read-time urgency of an open work order (Draft, Assigned, or InProgress) whose due date is before today in America/Chicago (`DueDateUrgency.Overdue` in [`src/Core/Model/DueDateUrgency.cs`](../../src/Core/Model/DueDateUrgency.cs)). A missing due date, a due date of today, and a Complete or Cancelled work order are not overdue; `WorkOrder.IsOverdue` in [`src/Core/Model/WorkOrder.cs`](../../src/Core/Model/WorkOrder.cs) is true only when `DueDateUrgencyCalculator.Calculate` returns that urgency. The same rule is described in [Work order due dates](../due-dates.md).
