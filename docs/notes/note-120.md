# Note 120

DueDateUrgencyPresenter.CssClass returns "due-date-today" for DueDateUrgency.DueToday, "due-date-overdue" for DueDateUrgency.Overdue, and an empty string otherwise (`DueDateUrgency.DueToday => "due-date-today", DueDateUrgency.Overdue => "due-date-overdue", _ => string.Empty`). The method is `DueDateUrgencyPresenter.CssClass` in `src/UI.Shared/Models/DueDateUrgencyPresenter.cs`.
