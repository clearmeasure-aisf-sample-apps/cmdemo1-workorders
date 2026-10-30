# Note 120

DueDateUrgencyCalculator.CssClass returns "due-date-today" for DueDateUrgency.DueToday, "due-date-overdue" for DueDateUrgency.Overdue, and an empty string otherwise (`DueDateUrgency.DueToday => "due-date-today", DueDateUrgency.Overdue => "due-date-overdue", _ => string.Empty`). The method is `DueDateUrgencyCalculator.CssClass` in `src/Core/Services/DueDateUrgencyCalculator.cs`.
