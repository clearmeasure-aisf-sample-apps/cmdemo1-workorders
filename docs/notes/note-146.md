# Note 146

DueDateUrgencyPresenter.ScreenReaderText returns "Due today" for DueDateUrgency.DueToday, "Overdue" for DueDateUrgency.Overdue, and null otherwise (`DueDateUrgency.DueToday => "Due today"`, `DueDateUrgency.Overdue => "Overdue"`, `_ => null`). The method is `DueDateUrgencyPresenter.ScreenReaderText` in `src/UI.Shared/Models/DueDateUrgencyPresenter.cs`.
