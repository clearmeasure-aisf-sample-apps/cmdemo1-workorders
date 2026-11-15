# Note 147

DueDateUrgencyPresenter.BadgeText returns "Overdue" for DueDateUrgency.Overdue, "Due Today" for DueDateUrgency.DueToday, "On Track" when the urgency is neither of those and a due date is present, and null when the urgency is neither of those and no due date is present (`DueDateUrgency.Overdue => "Overdue"`, `DueDateUrgency.DueToday => "Due Today"`, `_ => dueDate.HasValue ? "On Track" : null`). The method is `DueDateUrgencyPresenter.BadgeText` in `src/UI.Shared/Models/DueDateUrgencyPresenter.cs`.
