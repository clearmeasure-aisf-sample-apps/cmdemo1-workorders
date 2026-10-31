# Note 147

DueDateUrgencyCalculator.BadgeText returns "Overdue" for DueDateUrgency.Overdue, "Due Today" for DueDateUrgency.DueToday, "On Track" when the urgency is neither of those and a due date is present, and null when the urgency is neither of those and no due date is present (`DueDateUrgency.Overdue => "Overdue"`, `DueDateUrgency.DueToday => "Due Today"`, `_ => dueDate.HasValue ? "On Track" : null`). The method is `DueDateUrgencyCalculator.BadgeText` in `src/Core/Services/DueDateUrgencyCalculator.cs`.
