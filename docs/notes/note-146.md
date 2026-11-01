# Note 146

DueDateUrgencyCalculator.ScreenReaderText returns "Due today" for DueDateUrgency.DueToday, "Overdue" for DueDateUrgency.Overdue, and null otherwise (`DueDateUrgency.DueToday => "Due today"`, `DueDateUrgency.Overdue => "Overdue"`, `_ => null`). The method is `DueDateUrgencyCalculator.ScreenReaderText` in `src/Core/Services/DueDateUrgencyCalculator.cs`.
