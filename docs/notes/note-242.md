# Note 242

`DueDateUrgency` in `src/Core/Model/DueDateUrgency.cs` is read-time urgency for an optional work-order due date and is never persisted. `None` is `0`, `DueToday` is `1`, and `Overdue` is `2`.
